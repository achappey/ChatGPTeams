using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;
public interface IRequestRepository
{
    Task<string> Create();
    Task Delete(string id);
    Task<Request> GetByToken(string token);
    Task DeleteByToken(string token);
}

public class RequestRepository : IRequestRepository
{
    private readonly string _siteId;
    private readonly ILogger<RequestRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    public RequestRepository(ILogger<RequestRepository> logger,
    AppConfig config, IMapper mapper,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<Request> GetByToken(string token)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIRequests, $"fields/{FieldNames.Title} eq '{token}'", $"{FieldNames.Title}");

        return _mapper.Map<Request>(item);
    }

    public async Task DeleteByToken(string token)
    {
        var item = await GetByToken(token);

        await GraphService.Sites[_siteId].Lists[ListNames.AIRequests].Items[item.Id]
        .Request()
        .DeleteAsync();
    }

    public async Task<string> Create()
    {
        var token = Guid.NewGuid().ToString();

        var newRequest = new Dictionary<string, object>()
        {
            {FieldNames.Title, token},
        }.ToListItem();

        await GraphService.Sites[_siteId].Lists[ListNames.AIRequests].Items
            .Request()
            .AddAsync(newRequest);

        return token;
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIRequests].Items[id]
         .Request()
         .DeleteAsync();
    }
}
