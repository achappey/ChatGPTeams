using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Repositories;
public interface IFunctionRepository
{
    Task<Function> Get(string id);
    Task<Function> GetByName(string name);
    Task<IEnumerable<Function>> GetAll();
}

public class FunctionRepository : IFunctionRepository
{
    private readonly string _siteId;
    private readonly ILogger<FunctionRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IMemoryCache _cache;

    public FunctionRepository(ILogger<FunctionRepository> logger,
    AppConfig config, IMapper mapper, IMemoryCache cache,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _graphClientFactory = graphClientFactory;
    }

    private GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Function>> GetAll()
    {
        return await _cache.GetOrCreateAsync($"Functions", async entry =>
             {
                 entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                 var items = await GraphService.GetAllListItemFromListAsync(_siteId, ListNames.AIFunctions);

                 return items.Select(a => _mapper.Map<Function>(a));
             });

    }

    public async Task<Function> GetByName(string name)
    {
        return await _cache.GetOrCreateAsync($"Functions.{name}", async entry =>
            {
                var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIFunctions, $"fields/{FieldNames.AIName} eq '{name}'");
                
                if (item != null)
                {
                    return _mapper.Map<Function>(item);
                }
                
                return null;

            });

    }

    public async Task<Function> Get(string id)
    {
        return await _cache.GetOrCreateAsync($"Functions.{id}", async entry =>
           {
               var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIFunctions, id);

               return _mapper.Map<Function>(item);
           });
    }

}
