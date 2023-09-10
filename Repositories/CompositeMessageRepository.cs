using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using AutoMapper;

namespace achappey.ChatGPTeams.Repositories;


public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetAllByConversation(ConversationContext context, Conversation conversation);
    Task<int> Create(Database.Models.Message message);
    Task Delete(int id);
    Task DeleteByConversationAndTeamsId(string conversationId, string teamsId);
    Task DeleteByConversationAndDateTime(string conversationId, DateTime date);
}


public class CompositeMessageRepository : IMessageRepository
{
    private readonly ISharePointMessageRepository _sharePointMessageRepository;
    private readonly ITeamsChatMessageRepository _teamsChatMessageRepository;
    private readonly ITeamsChannelMessageRepository _teamsChannelMessageRepository;
    private readonly IMapper _mapper;

    public CompositeMessageRepository(ISharePointMessageRepository sharePointMessageRepository,
                                      ITeamsChatMessageRepository teamsChatMessageRepository,
                                      IMapper mapper,
                                      ITeamsChannelMessageRepository teamsChannelMessageRepository)
    {
        _sharePointMessageRepository = sharePointMessageRepository;
        _teamsChatMessageRepository = teamsChatMessageRepository;
        _teamsChannelMessageRepository = teamsChannelMessageRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Message>> GetAllByConversation(ConversationContext context, Conversation conversation)
    {
        var messages = await _sharePointMessageRepository.GetByConversation(conversation.Id);
        var messageList = messages.Select(_mapper.Map<Message>).ToList();

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

    public async Task<int> Create(Database.Models.Message message)
    {
        return await _sharePointMessageRepository.Create(message);
    }

    public async Task DeleteByConversationAndDateTime(string conversationId, DateTime date)
    {
        await _sharePointMessageRepository.DeleteByConversationAndDateTime(conversationId, date);
    }

    public async Task Delete(int id)
    {
        await _sharePointMessageRepository.Delete(id);
    }

    public async Task DeleteByConversationAndTeamsId(string conversationId, string teamsId)
    {
        await _sharePointMessageRepository.DeleteByConversationAndTeamsId(conversationId, teamsId);
    }
}
