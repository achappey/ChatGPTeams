using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IUserRepository
{

    Task<User> Get(int id);
    Task<User> GetByAadObjectId(string id);
    Task<User> GetCurrent();
    Task<IEnumerable<User>> GetAll();
    Task<bool> IsMemberOf(string groupId);

}

public class UserRepository : IUserRepository
{
    private readonly string _siteId;
    private readonly ILogger<UserRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IMemoryCache _cache;

    public UserRepository(ILogger<UserRepository> logger, AppConfig config,
    IMapper mapper, IGraphClientFactory graphClientFactory, IMemoryCache cache)
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

    public async Task<IEnumerable<User>> GetAll()
    {
        return await _cache.GetOrCreateAsync($"Users", async entry =>
               {
                   var items = await GraphService.GetAllListItemFromListAsync(_siteId, "User Information List");
                   return items.Select(a => _mapper.Map<User>(a));
               });
    }

    public async Task<User> Get(int id)
    {
        return await _cache.GetOrCreateAsync($"User_{id}", async entry =>
               {
                   var item = await GraphService.GetListItemFromListAsync(_siteId, "User Information List", id.ToString());
                   return _mapper.Map<User>(item);
               });
    }

    public async Task<bool> IsMemberOf(string groupId)
    {
        var groups = await GraphService.Me.MemberOf.Request().Select("mail").GetAsync();

        return groups.CurrentPage.OfType<Microsoft.Graph.Group>().Any(g => g.Mail == groupId);
    }


    public async Task<User> GetCurrent()
    {
        var item = await GraphService.Me.Request().GetAsync();

        return await GetByGraphUser(item);
    }

    public async Task<User> GetByAadObjectId(string id)
    {
        var item = await GraphService.Users[id].Request().GetAsync();

        return await GetByGraphUser(item);
    }

    private async Task<User> GetByGraphUser(Microsoft.Graph.User user)
    {
        var sharePointUser = await GraphService.GetFirstListItemFromListAsync(_siteId, "User Information List", $"fields/EMail eq '{user.Mail}'");

        return new User
        {
            Department = user.Department.NameToDepartment(),
            Id = sharePointUser.Id.ToInt().Value,
            Mail = user.Mail,
            DisplayName = user.DisplayName,
            AadObjectId = user.Id
        };
    }


}
