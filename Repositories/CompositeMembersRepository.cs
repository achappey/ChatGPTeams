using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;

namespace achappey.ChatGPTeams.Repositories;

public interface IMembersRepository
{
    Task<IEnumerable<User>> GetAllByConversation(ConversationContext context, Conversation conversation);
}

public class CompositeMembersRepository : IMembersRepository
{
    private readonly IUserRepository _userRepository;
    private readonly ITeamsChatMembersRepository _teamsChatMembersRepository;
    private readonly ITeamsChannelMembersRepository _teamsChannelMembersRepository;

    public CompositeMembersRepository(IUserRepository userRepository,
                                      ITeamsChatMembersRepository teamsChatMembersRepository,
                                      ITeamsChannelMembersRepository teamsChannelMembersRepository)
    {
        _userRepository = userRepository;
        _teamsChatMembersRepository = teamsChatMembersRepository;
        _teamsChannelMembersRepository = teamsChannelMembersRepository;
    }

    public async Task<IEnumerable<User>> GetAllByConversation(ConversationContext context, Conversation conversation)
    {
        var memberList = new List<User>();

        switch (context.ChatType)
        {
            case ChatType.channel:
                memberList.AddRange(await _teamsChannelMembersRepository.GetAll(context.TeamsId, context.ChannelId));
                break;
            case ChatType.groupchat:
                memberList.AddRange(await _teamsChatMembersRepository.GetAll(context.Id));
                break;
            case ChatType.personal:
                memberList.Add(await _userRepository.GetCurrent());
                break;
        }

        return memberList.OrderBy(a => a.DisplayName);
    }
}
