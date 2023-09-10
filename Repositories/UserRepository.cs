using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IUserRepository
{

    Task<User> Get(string id);
    Task<User> GetCurrent();
    Task<bool> IsMemberOf(string groupId);
    Task<IEnumerable<User>> GetAll();

}

public class UserRepository : IUserRepository
{
    private readonly string _siteId;
    private readonly ILogger<UserRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ChatGPTeamsContext _context;
    private readonly IMemoryCache _cache;

    public UserRepository(
        ILogger<UserRepository> logger,
        AppConfig config,
        IMapper mapper,
        IGraphClientFactory graphClientFactory,
        IMemoryCache cache,
        ChatGPTeamsContext context) // Add this line
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _graphClientFactory = graphClientFactory;
        _context = context; // Add this line
    }


    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<bool> IsMemberOf(string groupId)
    {
        var groups = await GraphService.Me.MemberOf.Request().Select("mail").GetAsync();

        return groups.CurrentPage.OfType<Microsoft.Graph.Group>().Any(g => g.Mail == groupId);
    }

    private async Task<Database.Models.User> EnsureUser(string id, string name)
    {
        var dbUser = await _context.GetByIdAsync<Database.Models.User>(id);

        if (dbUser == null)
        {
            dbUser = new Database.Models.User()
            {
                Id = id,
                Name = name.ToChatHandle()

            };

            await _context.AddAsync(dbUser);
        }

        return dbUser;
    }

    private async Task<User> MergeUser(Microsoft.Graph.User graphUser, Database.Models.User dbUser)
    {
        Department department = null;

        if (!string.IsNullOrEmpty(graphUser.Department))
        {
            var dbDepartment = await _context.Departments.FirstOrDefaultAsync(a => a.Name == graphUser.Department);

            if (dbDepartment == null)
            {
                dbDepartment = new Database.Models.Department()
                {
                    Name = graphUser.Department
                };

                await _context.AddAsync(dbDepartment);

                dbDepartment = await _context.Departments.FirstOrDefaultAsync(a => a.Name == graphUser.Department);
            }

            department = _mapper.Map<Department>(dbDepartment);
        }

        return new User()
        {
            DisplayName = graphUser.DisplayName,
            Id = graphUser.Id,
            Name = dbUser.Name,
            Mail = graphUser.Mail,
            Department = department
        };

    }

    public async Task<IEnumerable<User>> GetAll()
    {
        return await _cache.GetOrCreateAsync("Users", async entry =>
   {
       entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // set cache expiration

       var items = await GraphService.Users.Request().Top(999).GetAsync();

       return items.Select(_mapper.Map<User>);
   });
    }




    public async Task<User> GetCurrent()
    {
        var item = await GraphService.Me.Request().GetAsync();

        var dbUser = await EnsureUser(item.Id, item.DisplayName);

        return await MergeUser(item, dbUser);
    }

    public async Task<User> Get(string id)
    {
        var item = await GraphService.Users[id].Request().GetAsync();
        var dbUser = await EnsureUser(item.Id, item.DisplayName);
        return await MergeUser(item, dbUser);
        //return await GetByGraphUser(item);
    }



}
