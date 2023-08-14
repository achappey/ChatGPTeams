using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IDepartmentRepository
{
    Task<Department> Get(string id);
    Task<Department> GetByName(string name);
    Task<IEnumerable<Department>> GetAll();
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly string _siteId;
    private readonly ILogger<DepartmentRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IMemoryCache _cache;

    public DepartmentRepository(ILogger<DepartmentRepository> logger,
    AppConfig config, IMapper mapper, IMemoryCache cache,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Department>> GetAll()
    {
        return await _cache.GetOrCreateAsync($"Departments", async entry =>
              {
                  var items = await GraphService.GetAllListItemFromListAsync(_siteId, ListNames.AIDepartments, $"{FieldNames.Title}");

                  return items.Select(a => _mapper.Map<Department>(a));
              });
    }

    public async Task<Department> GetByName(string name)
    {
        var items = await GetAll();

        return items.FirstOrDefault(a => a.Name == name);
    }

    public async Task<Department> Get(string id)
    {
        var items = await GetAll();

        return items.FirstOrDefault(a => a.Id == id);
    }

}
