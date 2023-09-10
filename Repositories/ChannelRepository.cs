using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IChannelRepository
{
    Task<Channel> Get(string id);
    Task<Channel> GetByTitleAndTeam(string title, string teamsId);
    Task<string> Create(Channel prompt);
    Task Update(Channel prompt);
}

public class ChannelRepository : IChannelRepository
{
    private readonly string _siteId;
    private readonly ILogger<ChannelRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.AITeam},{FieldNames.AITeam.ToLookupField()}";

    public ChannelRepository(ILogger<ChannelRepository> logger,
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

    public async Task<Channel> GetByTitleAndTeam(string title, string teamsId)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIChannels,
            $"fields/{FieldNames.Title} eq '{title}' and fields/{FieldNames.AITeam.ToLookupField()} eq {teamsId.ToInt()}", _selectQuery);

        return _mapper.Map<Channel>(item);
    }

    public async Task<Channel> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIChannels, id);

        return _mapper.Map<Channel>(item);
    }

    public async Task<string> Create(Channel item)
    {
        var newItem = new Dictionary<string, object>()
        {
            {FieldNames.Title, item.Title },
            {FieldNames.AIAssistant.ToLookupField(), item.Assistant?.Id},
            {FieldNames.AITeam.ToLookupField(), item.Team.Id},
        }.ToListItem();

        var createdItem = await GraphService.Sites[_siteId].Lists[ListNames.AIChannels].Items
            .Request()
            .AddAsync(newItem);

        return createdItem.Id;
    }

    public async Task Update(Channel item)
    {
        var itemToUpdate = new Dictionary<string, object>()
        {
            {FieldNames.AIAssistant.ToLookupField(), item.Assistant?.Id},
        }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIChannels].Items[item.Id].Fields
            .Request()
            .UpdateAsync(itemToUpdate);
    }
}
