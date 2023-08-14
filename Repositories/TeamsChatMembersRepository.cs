using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ITeamsChatMembersRepository
{
    Task<IEnumerable<User>> GetAll(string conversationId);
}

public class TeamsChatMembersRepository : ITeamsChatMembersRepository
{
    private readonly ILogger<TeamsChatMembersRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    public TeamsChatMembersRepository(ILogger<TeamsChatMembersRepository> logger, IMapper mapper, IGraphClientFactory graphClientFactory)
    {
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

    public async Task<IEnumerable<User>> GetAll(string chatId)
    {
        var members = await GraphService.Chats[chatId].Members.Request().GetAsync();

        return members.Select(a => _mapper.Map<User>(a));
    }
}
