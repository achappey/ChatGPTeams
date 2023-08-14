using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Bot.Schema;
using System;

namespace achappey.ChatGPTeams.Services;

public interface IConversationService
{
    Task<Conversation> GetConversationByContextAsync(ConversationContext context);
    Task EnsureConversationByReferenceAsync(ConversationReference reference);
    Task UpdateConversationAsync(Conversation conversation);
    Task<string> GetConversationIdByContextAsync(ConversationContext context);
    Task DeleteConversationAsync(ConversationReference reference);
    Task ClearConversationHistoryAsync(ConversationContext context, int? messagesToKeep);
    Task ChangeConversationAssistantAsync(ConversationContext context, string assistantName);

        
}

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAssistantService _assistantService;
    private readonly IResourceRepository _resourceRepository;
    private readonly IMessageRepository _messageRepository;

    public ConversationService(IConversationRepository conversationRepository, IResourceRepository resourceRepository,
        IAssistantService assistantService, IMessageRepository messageRepository)
    {
        _conversationRepository = conversationRepository;
        _assistantService = assistantService;
        _messageRepository = messageRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<Conversation> GetConversationByContextAsync(ConversationContext context)
    {
        var conversation = await _conversationRepository.GetByTitle(context.Id);

        conversation.Assistant = await _assistantService.GetAssistant(conversation.Assistant.Id);
        conversation.Resources = await _resourceRepository.GetByConversation(conversation.Id);

        return conversation;
    }

    public async Task EnsureConversationByReferenceAsync(ConversationReference reference)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        if (conversation == null)
        {
            var assistants = await _assistantService.GetMyAssistants();
            var assistant = assistants.FirstOrDefault();
            await _conversationRepository.Create(new Conversation()
            {
                Title = reference.Conversation.Id,
                Temperature = assistant.Temperature,
                Assistant = assistant
            });
        }
    }

    public async Task ClearConversationHistoryAsync(ConversationContext context, int? messagesToKeep)
    {
        var date = DateTime.Now;
        var conversation = await GetConversationByContextAsync(context);

        if (messagesToKeep.HasValue && messagesToKeep.Value >= 0)
        {
            var messages = await _messageRepository.GetAllByConversation(context, conversation);

            // Ensure messages are sorted by creation date, then take the required message
            if (messages != null && messages.Count() > messagesToKeep.Value)
            {
                var sortedMessages = messages.OrderByDescending(m => m.Created).ToList();

                // If value is 0, you take the last message, if it's 5 you take the fifth from last, etc.
                var messageToKeep = sortedMessages.Skip(messagesToKeep.Value).FirstOrDefault();
                if (messageToKeep != null)
                {
                    date = messageToKeep.Created.Value.DateTime;
                }
            }
        }

        if (context.ChatType == ChatType.personal)
        {
            await _messageRepository.DeleteByConversationAndDateTime(conversation.Id, date);

        }

        conversation.CutOff = date;

        await _conversationRepository.Update(conversation);
    }
 
    public async Task<string> GetConversationIdByContextAsync(ConversationContext context)
    {
        var conversation = await _conversationRepository.GetByTitle(context.Id);

        return conversation.Id;
    }

    public async Task ChangeConversationAssistantAsync(ConversationContext context, string assistantId)
    {
        var assistant = await _assistantService.GetAssistant(assistantId);
        var conversation = await _conversationRepository.GetByTitle(context.Id);
        conversation.Assistant = assistant;
        conversation.Temperature = assistant.Temperature;

        await _conversationRepository.Update(conversation);
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        await _conversationRepository.Update(conversation);
    }


    public async Task DeleteConversationAsync(ConversationReference reference)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        await _conversationRepository.Delete(conversation.Id);
    }
}
