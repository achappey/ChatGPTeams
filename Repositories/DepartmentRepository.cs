using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Database.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using achappey.ChatGPTeams.Database;
using Microsoft.EntityFrameworkCore;

namespace achappey.ChatGPTeams.Repositories;

public interface IDepartmentRepository
{
    Task<Department> Get(int id);
    Task<Department> GetByName(string name);
    Task<IEnumerable<Department>> GetAll();
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly string _siteId;
    private readonly ILogger<DepartmentRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ChatGPTeamsContext _context;  // Add this line
    private readonly IMemoryCache _cache;

    public DepartmentRepository(ILogger<DepartmentRepository> logger,
        AppConfig config, IMapper mapper, IMemoryCache cache,
        IGraphClientFactory graphClientFactory, ChatGPTeamsContext context)  // Add YourDbContext here
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _graphClientFactory = graphClientFactory;
        _context = context;  // Initialize the context
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
        return await _context.Departments.ToListAsync();
    }

    public async Task<Department> GetByName(string name)
    {
        return await _context.Departments.FirstOrDefaultAsync(a => a.Name == name);
    }

    public async Task<Department> Get(int id)
    {
        return await _context.Departments.FirstOrDefaultAsync(a => a.Id == id);
    }
    /*
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

        public async Task<Department> Get(int id)
        {
            var items = await GetAll();

            return items.FirstOrDefault(a => a.Id == id);
        }
    */
}
