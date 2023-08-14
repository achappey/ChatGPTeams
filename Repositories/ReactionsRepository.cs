using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IReactionsRepository
{
    Task<Reaction> Get(string id);
    Task<Reaction> GetByTitle(string title);
    Task<IEnumerable<Reaction>> GetAll();
    Task<string> Create(Reaction ractions);
}

public class ReactionsRepository : IReactionsRepository
{
    private readonly string _siteId;
    private readonly ILogger<DepartmentRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.Title}";

    public ReactionsRepository(ILogger<DepartmentRepository> logger,
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

    public async Task<IEnumerable<Reaction>> GetAll()
    {
        var items = await GraphService.GetAllListItemFromListAsync(_siteId, ListNames.AIReactions, $"{FieldNames.Title}");

        return items.Select(a => _mapper.Map<Reaction>(a));
    }

    public async Task<Reaction> GetByTitle(string title)
    {
         var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIReactions, $"fields/{FieldNames.Title} eq '{title}'", _selectQuery);
        
        return _mapper.Map<Reaction>(item);
    }

    public async Task<Reaction> Get(string id)
    {
         var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIReactions, id, _selectQuery);

         return _mapper.Map<Reaction>(item);
    }

      public async Task<string> Create(Reaction reaction)
    {
        var newReaction = new Dictionary<string, object>()
                    {
                        {FieldNames.Title, reaction.Title}
                    }.ToListItem();

        var createdReaction = await GraphService.Sites[_siteId].Lists[ListNames.AIReactions].Items
            .Request()
            .AddAsync(newReaction);

        return createdReaction.Id;
    }

}
