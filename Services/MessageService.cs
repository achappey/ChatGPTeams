using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IMessageService
{
    Task<int> CreateMessageAsync(Message message);
    Task DeleteMessageById(int id);
    Task DeleteByConversationAndTeamsId(string conversationId, string messageId);
    Task<IEnumerable<Message>> GetByConversationAsync(ConversationContext context, string conversationId);
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationService _conversationService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public MessageService(IConversationRepository conversationRepo, IUserRepository userRepository,
    IMapper mapper,
    IMessageRepository messageRepository, IConversationService conversationService)
    {
        _messageRepository = messageRepository;
        _mapper = mapper;
        _conversationService = conversationService;
        _userRepository = userRepository;
    }


    public async Task<IEnumerable<Message>> GetByConversationAsync(ConversationContext context, string conversationId)
    {
        var conversation = await _conversationService.GetConversationAsync(conversationId);
        return await _messageRepository.GetAllByConversation(context, conversation);
    }

    public async Task<int> CreateMessageAsync(Message message)
    {
        var conversation = await _conversationService.GetConversationAsync(message.Reference.Conversation.Id);
        message.ConversationId = conversation.Id;

        if (message.Role == Role.user)
        {
            var user = await _userRepository.Get(message.Reference.User.AadObjectId);

            message.Name = user.Name;
        }

        return await _messageRepository.Create(_mapper.Map<Database.Models.Message>(message));
    }


    public async Task DeleteByConversationAndTeamsId(string conversationId, string messageId)
    {
        var conversation = await _conversationService.GetConversationAsync(conversationId);

        await _messageRepository.DeleteByConversationAndTeamsId(conversation.Id, messageId);
    }


    public async Task DeleteMessageById(int id)
    {
        await _messageRepository.Delete(id);
    }

}
