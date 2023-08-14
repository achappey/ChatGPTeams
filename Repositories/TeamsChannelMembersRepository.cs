using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ITeamsChannelMembersRepository
{
    Task<IEnumerable<User>> GetAll(string teamsId, string channelId);
}

public class TeamsChannelMembersRepository : ITeamsChannelMembersRepository
{
    private readonly ILogger<TeamsChatMembersRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    public TeamsChannelMembersRepository(ILogger<TeamsChatMembersRepository> logger, IMapper mapper, IGraphClientFactory graphClientFactory)
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

    public async Task<IEnumerable<User>> GetAll(string teamsId, string channelId)
    {
        var members = await GraphService.Teams[teamsId].Channels[channelId].Members.Request().GetAsync();

        return members.Select(a => _mapper.Map<User>(a));
    }
}
