using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Repositories;

public interface IResourceRepository
{
    Task<Resource> Get(string id);
    Task<IEnumerable<Resource>> GetByConversation(string conversationId);
    Task<IEnumerable<Resource>> GetByAssistant(string assistantId);
    Task<string> Create(Resource resource);
    Task<IEnumerable<string>> Read(Resource resource);
    Task Delete(string id);
    Task<string> GetFileName(Resource resource);
    Task Update(Resource resource);
}

public class ResourceRepository : IResourceRepository
{
    private readonly string _siteId;
    private readonly ILogger<ResourceRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.AIConversation.ToLookupField()},{FieldNames.AIContentUrl}";

    public ResourceRepository(ILogger<ResourceRepository> logger,
    AppConfig config, IMapper mapper, IHttpClientFactory httpClientFactory,
    IGraphClientFactory graphClientFactory)
    {
        _logger = logger;
        _mapper = mapper;
        _siteId = config.SharePointSiteId;
        _httpClientFactory = httpClientFactory;
        _graphClientFactory = graphClientFactory;
    }

    private GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<Resource> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIResources, id, _selectQuery);

        return _mapper.Map<Resource>(item);
    }

    public async Task<IEnumerable<Resource>> GetByConversation(string conversationId)
    {
        var items = await GraphService.GetListItemsFromListAsync(_siteId,
        ListNames.AIResources,
        $"fields/{FieldNames.AIConversation.ToLookupField()} eq {conversationId.ToInt()}",
        _selectQuery);

        return _mapper.Map<IEnumerable<Resource>>(items);
    }

    public async Task<IEnumerable<Resource>> GetByAssistant(string assistantId)
    {
        var items = await GraphService.GetListItemsFromListAsync(_siteId,
        ListNames.AIResources,
        $"fields/{FieldNames.AIAssistant.ToLookupField()} eq {assistantId.ToInt()}",
        _selectQuery);

        return _mapper.Map<IEnumerable<Resource>>(items);
    }

    public async Task<string> GetFileName(Resource resource)
    {
        if (resource.Url.IsSharePointUrl())
        {
            try
            {
                var driveItem = await GraphService.GetDriveItem(resource.Url);
                return driveItem.Name;

            }
            catch (ServiceException e)
            {
                if (e.Error.Message == "Site Pages cannot be accessed as a drive item")
                {
                    var (_, _, PageName) = resource.Url.ExtractSharePointValues();
                    return PageName;
                }

                return null;
            }
        }
        else
        {
            return resource.Name;
        }
    }

    private async Task<IEnumerable<string>> FindPage(Resource resource)
    {
        var (Hostname, Path, PageName) = resource.Url.ExtractSharePointValues();

        var site = await GraphService.Sites[Hostname + ":/sites/" + Path]
            .Request()
            .Select("id")
            .GetAsync();

        var pages = await GraphService.Sites[site.Id].Pages
            .Request()
            .Filter($"name eq '{PageName}'")
            .GetAsync();

        if (pages.Count() > 0)
        {
            return await ExtractPage(site.Id, pages.First().Id);
        }

        return null;
    }

    private async Task<IEnumerable<string>> ExtractPage(string siteId, string pageId)
    {
        string requestUrl = GraphService.BaseUrl + $"/sites/{siteId}/pages/{pageId}/microsoft.graph.sitePage?expand=canvasLayout";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var response = await GraphService.HttpProvider.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        var sitePageData = JsonConvert.DeserializeObject<Models.SitePage>(text);

        List<string> allInnerHtml = sitePageData.CanvasLayout.HorizontalSections
            .SelectMany(hs => hs.Columns)
            .SelectMany(c => c.Webparts)
            .Select(wp => wp.InnerHtml)
            .ToList();

        if (sitePageData.CanvasLayout.VerticalSections != null)
        {
            allInnerHtml.AddRange(sitePageData.CanvasLayout.VerticalSections
                .Where(vs => vs.Columns != null)
                .SelectMany(vs => vs.Columns)
                .SelectMany(c => c.Webparts)
                .Select(wp => wp.InnerHtml));
        }

        return allInnerHtml.Where(a => !string.IsNullOrEmpty(a));
    }

    public async Task<IEnumerable<string>> Read(Resource resource)
    {
        if (resource.Url.IsSharePointUrl() || resource.Url.IsOutlookUrl())
        {
            try
            {
                var driveItem = await GraphService.GetDriveItem(resource.Url);
                var driveContent = await GraphService.GetDriveItemContent(driveItem.ParentReference.DriveId, driveItem.Id);
                var lines = resource.Name.GetLinesAsync(driveContent);
                return lines;

            }
            catch (ServiceException e)
            {
                if (e.Error.Message == "Site Pages cannot be accessed as a drive item")
                {
                    return await FindPage(resource);
                }

                //  throw e;
                return null;
            }

            //"Site Pages cannot be accessed as a drive item"

        }
        else
        {
            return await ConvertPageToList(resource.Url);
        }
    }

    public async Task<string> Create(Resource resource)
    {
        var newResource = new Dictionary<string, object>()
        {
            {FieldNames.Title, resource.Name},
            {FieldNames.AIConversation.ToLookupField(), resource.Conversation.Id.ToInt()},
            {FieldNames.AIContentUrl,resource.Url},
        }.ToListItem();

        var createdResource = await GraphService.Sites[_siteId].Lists[ListNames.AIResources].Items
            .Request()
            .AddAsync(newResource);

        return createdResource.Id;
    }

    public async Task Update(Resource resource)
    {
        var updateResource = new Dictionary<string, object>()
        {
            {FieldNames.Title, resource.Name},
            {FieldNames.AIConversation.ToLookupField(), resource.Conversation?.Id.ToInt()},
            {FieldNames.AIAssistant.ToLookupField(), resource.Assistant?.Id.ToInt()},
            {FieldNames.AIContentUrl, resource.Url},
        }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIResources].Items[resource.Id].Fields
            .Request()
            .UpdateAsync(updateResource);
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIResources].Items[id]
         .Request()
         .DeleteAsync();
    }

    private List<string> ExtractTextFromHtmlParagraphs(HtmlDocument htmlDoc)
    {
        var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p");
        var pageParagraphs = new List<string>();

        if (paragraphs is not null)
        {
            foreach (var paragraph in paragraphs)
            {
                if (paragraph.InnerText is not null)
                {
                    var text = paragraph.InnerText.Trim();

                    if (!string.IsNullOrEmpty(text))
                    {
                        pageParagraphs.Add(text);
                    }
                }
            }
        }

        return pageParagraphs;
    }

    private async Task<List<string>> ConvertPageToList(string url)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var htmlContent = await httpClient.GetStringAsync(url);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlContent);

        return ExtractTextFromHtmlParagraphs(htmlDocument);

    }
}
