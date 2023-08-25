using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;

namespace achappey.ChatGPTeams.Services;

public interface IMessageService
{
    Task<string> CreateMessageAsync(Message message);
    Task DeleteMessageById(string id);
    Task DeleteByConversationAndTeamsId(string conversationId, string messageId);
    Task<IEnumerable<Message>> GetByConversationAsync(ConversationContext context, string conversationId);
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepo;
    private readonly IUserRepository _userRepository;

    public MessageService(IConversationRepository conversationRepo, IUserRepository userRepository,
    IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepo = conversationRepo;
        _userRepository = userRepository;
    }


    public async Task<IEnumerable<Message>> GetByConversationAsync(ConversationContext context, string conversationId)
    {
        var conversation = await _conversationRepo.Get(conversationId);
        return await _messageRepository.GetAllByConversation(context, conversation);
    }

    public async Task<string> CreateMessageAsync(Message message)
    {
        var conversation = await _conversationRepo.GetByTitle(message.Reference.Conversation.Id);
        message.ConversationId = conversation.Id;

        if (message.Role == Role.user)
        {
            var user = await _userRepository.GetByAadObjectId(message.Reference.User.AadObjectId);

            message.Name = user.DisplayName.ToChatHandle();
        }

        return await _messageRepository.Create(message);
    }


    public async Task DeleteByConversationAndTeamsId(string conversationId, string messageId)
    {
        var conversation = await _conversationRepo.GetByTitle(conversationId);

        await _messageRepository.DeleteByConversationAndTeamsId(conversation.Id, messageId);
    }


    public async Task DeleteMessageById(string id)
    {
        await _messageRepository.Delete(id);
    }

}
