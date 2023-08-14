using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;

namespace achappey.ChatGPTeams.Repositories;


public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetAllByConversation(ConversationContext context, Conversation conversation);
    Task<string> Create(Message message);
    Task Update(Message message);
    Task Delete(string id);
    Task DeleteByConversationAndTeamsId(string conversationId, string teamsId);
    Task<Message> GetByConversationAndTeamsId(string conversationId, string teamsId);
    Task DeleteByConversationAndDateTime(string conversationId, DateTime date);
}


public class CompositeMessageRepository : IMessageRepository
{
    private readonly ISharePointMessageRepository _sharePointMessageRepository;
    private readonly ITeamsChatMessageRepository _teamsChatMessageRepository;
    private readonly ITeamsChannelMessageRepository _teamsChannelMessageRepository;

    public CompositeMessageRepository(ISharePointMessageRepository sharePointMessageRepository,
                                      ITeamsChatMessageRepository teamsChatMessageRepository,
                                      ITeamsChannelMessageRepository teamsChannelMessageRepository)
    {
        _sharePointMessageRepository = sharePointMessageRepository;
        _teamsChatMessageRepository = teamsChatMessageRepository;
        _teamsChannelMessageRepository = teamsChannelMessageRepository;
    }

    public async Task<IEnumerable<Message>> GetAllByConversation(ConversationContext context, Conversation conversation)
    {
        var messages = await _sharePointMessageRepository.GetByConversation(conversation.Id);
        var messageList = messages.ToList();

        switch (context.ChatType)
        {
            case ChatType.channel:
                messageList.AddRange(await _teamsChannelMessageRepository.GetByConversation(context.TeamsId, context.ChannelId, context.MessageId));
                break;
            case ChatType.groupchat:
                messageList.AddRange(await _teamsChatMessageRepository.GetByConversation(context.Id));
                break;
        }

        if (conversation.CutOff.HasValue)
        {
            messageList = messageList.Where(t => t.Created.Value > conversation.CutOff.Value).ToList();
        }

        return messageList.OrderBy(a => a.Created);
    }

    public async Task<string> Create(Message message)
    {
        return await _sharePointMessageRepository.Create(message);
    }

    public async Task DeleteByConversationAndDateTime(string conversationId, DateTime date)
    {
        await _sharePointMessageRepository.DeleteByConversationAndDateTime(conversationId, date);
    }

    public async Task Delete(string id)
    {
        await _sharePointMessageRepository.Delete(id);
    }

    public async Task DeleteByConversationAndTeamsId(string conversationId, string teamsId)
    {
        await _sharePointMessageRepository.DeleteByConversationAndTeamsId(conversationId, teamsId);
    }

    public async Task<Message> GetByConversationAndTeamsId(string conversationId, string teamsId)
    {
        return await _sharePointMessageRepository.GetByConversationAndTeamsId(conversationId, teamsId);
    }

    public async Task Update(Message message)
    {
        await _sharePointMessageRepository.Update(message);

    }
}
