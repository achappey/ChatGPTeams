using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;

public interface ITeamsService
{
    Task<Assistant> GetDefaultAssistant(string teamsId, string channelId);
    Task<Teams> UpdateTeamsAssistant(string teamsId, Assistant assistant);
    Task<Channel> UpdateChannelAssistant(string teamsId, string channelId, Assistant assistant);
}

public class TeamsService : ITeamsService
{
    private readonly ITeamsRepository _teamsRepository;
    private readonly IChannelRepository _channelRepository;

    public TeamsService(ITeamsRepository teamsRepository, IChannelRepository channelRepository)
    {
        _teamsRepository = teamsRepository;
        _channelRepository = channelRepository;
    }

    public async Task<Assistant> GetDefaultAssistant(string teamsId, string channelId)
    {
        var teamsItem = await _teamsRepository.GetByTitle(teamsId);

        if (teamsItem != null)
        {
            var channelItem = await _channelRepository.GetByTitleAndTeam(channelId, teamsItem.Id);

            return channelItem.Assistant ?? teamsItem.Assistant;
        }

        return null;
    }

    public async Task<Channel> UpdateChannelAssistant(string teamsId, string channelId, Assistant assistant)
    {
        var teamsItem = await EnsureTeams(teamsId);
        var channelItem = await _channelRepository.GetByTitleAndTeam(channelId, teamsItem.Id);

        if (channelItem != null)
        {
            channelItem.Assistant = assistant;

            await _channelRepository.Update(channelItem);
        }
        else
        {
            channelItem = new Channel()
            {
                Title = channelId,
                Team = teamsItem,
                Assistant = assistant
            };

            channelItem.Id = await _channelRepository.Create(channelItem);
        }

        return channelItem;
    }

    public async Task<Teams> UpdateTeamsAssistant(string teamsId, Assistant assistant)
    {
        var teamsItem = await EnsureTeams(teamsId);
        teamsItem.Assistant = assistant;
        await _teamsRepository.Update(teamsItem);

        return teamsItem;
    }

    private async Task<Teams> EnsureTeams(string teamsId)
    {
        var teamsItem = await _teamsRepository.GetByTitle(teamsId);

        if (teamsItem == null)
        {
            teamsItem = new Teams()
            {
                Title = teamsId
            };

            teamsItem.Id = await _teamsRepository.Create(teamsItem);
        }

        return teamsItem;
    }

}
