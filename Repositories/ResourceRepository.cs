using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Database.Models;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
//using Microsoft.Graph;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Repositories;

public interface IResourceRepository
{
    Task<Resource> Get(string id);
    Task<IEnumerable<Resource>> GetByConversation(string conversationId);
    // Task<IEnumerable<Resource>> GetByAssistant(int assistantId);
    Task<int> Create(Resource resource);
    Task<IEnumerable<string>> Read(Resource resource);
    Task Delete(int id);
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
    private readonly ChatGPTeamsContext _context;

    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.AIConversation.ToLookupField()},{FieldNames.AIContentUrl}";

    public ResourceRepository(ILogger<ResourceRepository> logger, ChatGPTeamsContext chatGPTeamsContext,
    AppConfig config, IMapper mapper, IHttpClientFactory httpClientFactory,
    IGraphClientFactory graphClientFactory)
    {
        _logger = logger;
        _mapper = mapper;
        _siteId = config.SharePointSiteId;
        _context = chatGPTeamsContext;
        _httpClientFactory = httpClientFactory;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    /*  public async Task<Resource> Get2(string id)
      {
          var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIResources, id, _selectQuery);

          return _mapper.Map<Resource>(item);
      }*/

    public async Task<Resource> Get(string id)
    {
        var item = await _context.Resources.FindAsync(id);
        return _mapper.Map<Resource>(item);
    }

    public async Task<IEnumerable<Resource>> GetByConversation(string conversationId)
    {
        return await _context.Resources.Where(t => t.Conversations.Any(y => y.Id == conversationId)
        || t.Assistants.Any(z => z.Conversations.Any(n => n.Id == conversationId))).ToListAsync();

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
            catch (Microsoft.Graph.ServiceException e)
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
            catch (Microsoft.Graph.ServiceException e)
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


    public async Task<int> Create(Resource resource)
    {

        try
        {
            await _context.Resources.AddAsync(resource);
            await _context.SaveChangesAsync();
            return resource.Id;
        }
        catch (DbUpdateException ex)
        {
            // Log the detailed error
            _logger.LogError(ex.InnerException?.Message ?? ex.Message);
            throw;
        }
    }

    public async Task Update(Resource resource)
    {
        var existingResource = await _context.Resources.FindAsync(resource.Id);
        if (existingResource != null)
        {
            _mapper.Map(resource, existingResource);
            _context.Resources.Update(existingResource);
            await _context.SaveChangesAsync();
        }
        // Handle not found scenario as needed.
    }

    public async Task Delete(int id)
    {
        var resourceToDelete = await _context.Resources.FindAsync(id);
        if (resourceToDelete != null)
        {
            _context.Resources.Remove(resourceToDelete);
            await _context.SaveChangesAsync();
        }
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
