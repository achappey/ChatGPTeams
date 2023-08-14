using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Bot.Schema;
using achappey.ChatGPTeams.Extensions;

namespace achappey.ChatGPTeams.Services;

public interface IMessageService
{
    Task<string> CreateMessageAsync(Message message);
    Task AddReactionToMessage(Reaction reaction, ConversationReference connectionReference);
    Task DeleteReactionFromMessage(string title, ConversationReference connectionReference);
    Task DeleteMessageById(string id);
    Task DeleteByConversationAndTeamsId(string conversationId, string messageId);
    Task<IEnumerable<Message>> GetByConversationAsync(ConversationContext context, string conversationId);
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IReactionService _reactionService;
    private readonly IConversationRepository _conversationRepo;
    private readonly IAssistantRepository _assistantRepo;
    private readonly IFunctionRepository _functionRepository;
    private readonly IUserRepository _userRepository;

    public MessageService(IConversationRepository conversationRepo, IAssistantRepository assistantRepository, IUserRepository userRepository,
    IMessageRepository messageRepository, IReactionService reactionService, IFunctionRepository functionRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepo = conversationRepo;
        _reactionService = reactionService;
        _assistantRepo = assistantRepository;
        _userRepository = userRepository;
        _functionRepository = functionRepository;
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

    public async Task AddReactionToMessage(Reaction reaction, ConversationReference connectionReference)
    {
        switch (connectionReference.Conversation.ConversationType)
        {
            case "personal":
                var item = await _reactionService.EnsureReaction(reaction.Title);

                var conversation = await _conversationRepo.GetByTitle(connectionReference.Conversation.Id);
                var messageItem = await _messageRepository.GetByConversationAndTeamsId(conversation.Id, connectionReference.ActivityId);

                if (messageItem != null)
                {
                    var reactions = messageItem.Reactions.ToList();

                    reactions.Add(item);
                    messageItem.Reactions = reactions;

                    await _messageRepository.Update(messageItem);
                }

                break;
            default:
                break;
        }


    }

    public async Task DeleteMessageById(string id)
    {
        await _messageRepository.Delete(id);
    }

    public async Task DeleteReactionFromMessage(string title, ConversationReference connectionReference)
    {
        var item = await _reactionService.EnsureReaction(title);

        var conversation = await _conversationRepo.GetByTitle(connectionReference.Conversation.Id);
        var messageItem = await _messageRepository.GetByConversationAndTeamsId(conversation.Id, connectionReference.ActivityId);

        if (messageItem != null)
        {
            messageItem.Reactions = messageItem.Reactions.Where(r => r.Id != item.Id);

            await _messageRepository.Update(messageItem);

        }
    }
}
