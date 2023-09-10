using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ITeamsRepository
{
    Task<Teams> Get(string id);
    Task<Teams> GetByTitle(string title);
    Task<string> Create(Teams prompt);
    Task Update(Teams prompt);
}

public class TeamsRepository : ITeamsRepository
{
    private readonly string _siteId;
    private readonly ILogger<TeamsRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()}";

    public TeamsRepository(ILogger<TeamsRepository> logger,
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

    public async Task<Teams> GetByTitle(string title)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AITeams, $"fields/{FieldNames.Title} eq '{title}'", _selectQuery);

        return _mapper.Map<Teams>(item);
    }

    public async Task<Teams> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AITeams, id);

        return _mapper.Map<Teams>(item);
    }


    public async Task<string> Create(Teams teams)
    {
        var newPrompt = new Dictionary<string, object>()
        {
            {FieldNames.Title, teams.Title },
            {FieldNames.AIAssistant.ToLookupField(), teams.Assistant?.Id},
        }.ToListItem();

        var createdItem = await GraphService.Sites[_siteId].Lists[ListNames.AITeams].Items
            .Request()
            .AddAsync(newPrompt);

        return createdItem.Id;
    }

    public async Task Update(Teams teams)
    {
        var promptToUpdate = new Dictionary<string, object>()
        {
            {FieldNames.AIAssistant.ToLookupField(), teams.Assistant?.Id},
        }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AITeams].Items[teams.Id].Fields
            .Request()
            .UpdateAsync(promptToUpdate);
    }
}
