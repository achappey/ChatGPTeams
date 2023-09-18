using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using achappey.ChatGPTeams.Models;
using System.Linq;
using Microsoft.Bot.Schema;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace achappey.ChatGPTeams.Services;

public interface IChatGPTeamsBotChatService
{

    Task ProcessMessageAsync(ConversationContext context,
                             Message message,
                             CancellationToken cancellationToken);
    Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm);

    Task DeleteMessageAsync(ConversationContext context,
                           ConversationReference reference,
                           string teamsId,
                           CancellationToken cancellationToken);
    Task ProcessAttachmentsAsync(ConversationContext context,
                                 ConversationReference reference,
                                 IEnumerable<Attachment> attachments,
                                 CancellationToken cancellationToken);
    Task EnsureConversationAsync(ConversationContext context);
    Task DeleteConversationByReferenceAsync(ConversationReference reference);
    Task ExecuteCustomPrompt(ConversationContext context, ConversationReference reference, int promptId,
            Message message, string user, string replyToId,
            bool keepContext, CancellationToken cancellationToken);


}

public class ChatGPTeamsBotChatService : IChatGPTeamsBotChatService
{
    private readonly IConversationService _conversationService;
    private readonly IResourceService _resourceService;
    private readonly IFunctionService _functionService;
    private readonly IPromptService _promptService;
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IFunctionExecutionService _functionExecutionService;

    private readonly IProactiveMessageService _proactiveMessageService;
    private readonly IMapper _mapper;


    public ChatGPTeamsBotChatService(IConversationService conversationService,
                                 IMapper mapper,
                                 IMessageService messageService,
                                 IPromptService promptService,
                                 IProactiveMessageService proactiveMessageService,
                                 IChatService chatService,
                                 IFunctionService functionService,
                                 IResourceService resourceService,
                                 IFunctionExecutionService functionExecutionService)
    {
        _conversationService = conversationService;
        _functionService = functionService;
        _functionExecutionService = functionExecutionService;
        _promptService = promptService;
        _messageService = messageService;
        _chatService = chatService;
        _resourceService = resourceService;
        _mapper = mapper;
        _proactiveMessageService = proactiveMessageService;
    }

    private async Task ExecuteFunctionAsync(ConversationContext context,
                                            ConversationReference reference,
                                            FunctionCall functionCall,
                                            CancellationToken cancellationToken)
    {
        var function = await _functionService.GetFunctionByNameAsync(functionCall.Name); // Retrieve function details

        string executionCardId = null;

        if (function != null)
        {
            executionCardId = await _proactiveMessageService.ExecuteFunctionAsync(reference, function, functionCall, cancellationToken); // Execute function
        }

        var result = await _functionExecutionService.ExecuteFunction(context, reference, functionCall);

        if (executionCardId != null && result != null)
        {
            await _proactiveMessageService.FunctionExecutedAsync(reference, function, functionCall, result, executionCardId, cancellationToken);
        }

        await ProcessChatStreamAsync(context, reference, cancellationToken); // Continue processing the chat conversation
    }

    public async Task ProcessMessageAsync(ConversationContext context, Message message, CancellationToken cancellationToken)
    {
        if (message.Reference.Conversation.ConversationType == "personal")
        {
            await _messageService.CreateMessageAsync(message);
        }

        await ProcessChatStreamAsync(context, message.Reference, cancellationToken);
    }

    private async Task ProcessChatStreamAsync(ConversationContext context,
                                              ConversationReference reference,
                                              CancellationToken cancellationToken)
    {
        bool isFirstMessage = true;
        string messageId = null;
        StringBuilder accumulatedContent = new StringBuilder();

        Message completeMessage = null;
        var conversation = await _chatService.GetChatConversation(context);
        int accumulatedMessageCount = 0;

        await foreach (var message in _chatService.SendRequestStream(conversation)) // Process each message in the stream
        {
            if (!string.IsNullOrEmpty(message.Content))
            {
                accumulatedContent.Append(message.Content);
                accumulatedMessageCount++;

                if (isFirstMessage)
                {
                    message.TeamsId = await _proactiveMessageService.SendMessageAsync(reference, accumulatedContent.ToString(), cancellationToken);
                    messageId = message.TeamsId;
                    isFirstMessage = false; // Reset flag
                }
                else if (accumulatedMessageCount >= 25)
                {
                    message.TeamsId = await _proactiveMessageService.UpdateMessageAsync(reference, accumulatedContent.ToString(), messageId, cancellationToken);
                    accumulatedMessageCount = 0;  // Reset the accumulated message count
                }

                if (completeMessage == null)
                {
                    completeMessage = message;
                }
            }

            if (message.FunctionCall != null)
            {
                // If there's a function call in the message, execute it
                await ExecuteFunctionAsync(context, reference, message.FunctionCall, cancellationToken);
            }
        }

        // If there are any remaining messages that were not batched to 20, update the card with them
        if (accumulatedMessageCount > 0 && !isFirstMessage)
        {
            await _proactiveMessageService.UpdateMessageAsync(reference, accumulatedContent.ToString(), messageId, cancellationToken);
        }

        // If conversation is personal and message content is not empty, save the message
        if (reference.Conversation.ConversationType == "personal" && !string.IsNullOrEmpty(accumulatedContent.ToString()))
        {
            completeMessage.Content = accumulatedContent.ToString();
            completeMessage.Reference = reference;
            completeMessage.Created = DateTimeOffset.Now;
            completeMessage.ConversationId = reference.Conversation.Id;
            completeMessage.Role = Role.assistant;

            await _messageService.CreateMessageAsync(completeMessage);
        }

        if (!string.IsNullOrEmpty(completeMessage.ContextQuery))
        {
            await _proactiveMessageService.UsedSourcesAsync(reference, completeMessage.ContextQuery, cancellationToken);
        }
    }

    public async Task ProcessAttachmentsAsync(ConversationContext context,
                                              ConversationReference reference,
                                              IEnumerable<Attachment> attachments,
                                              CancellationToken cancellationToken)
    {
        var items = attachments.SelectMany(a => _mapper.Map<IEnumerable<Resource>>(a))
                                .Where(a => !string.IsNullOrEmpty(a.Url) && !string.IsNullOrEmpty(a.Name))
                                .GroupBy(a => a.Url)
                                .Select(g => g.First())
                                .ToList();

        items.AddRange(await this._messageService.GetResourcesByContext(context));

        var currentResources = await _resourceService.GetResourcesByConversation(context.Id);

        foreach (var item in items.Where(a => !currentResources.Any(y => y.Url == a.Url)))
        {
            var cardId = await _proactiveMessageService.ImportResourceAsync(reference, item, cancellationToken);
            var lineCount = await _resourceService.ImportResourceAsync(reference, item);

            await _proactiveMessageService.ImportResourceFinishedAsync(reference, item, lineCount, cardId, cancellationToken);
        }
    }

    public async Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm)
    {
        return await _promptService.GetPromptByContentAsync(searchTerm);
    }

    public async Task<IEnumerable<Prompt>> GetAllPrompts()
    {
        return await _promptService.GetMyPromptsAsync();
    }

    public async Task EnsureConversationAsync(ConversationContext context)
    {
        await _conversationService.EnsureConversationAsync(context.Id, context.TeamsId, context.ChannelId);
    }

    public async Task DeleteConversationByReferenceAsync(ConversationReference reference)
    {
        await _conversationService.DeleteConversationAsync(reference);
    }


    public async Task DeleteMessageAsync(ConversationContext context,
                                       ConversationReference reference,
                                       string teamsId,
                                       CancellationToken cancellationToken)
    {
        await _messageService.DeleteByConversationAndTeamsId(reference.Conversation.Id, teamsId);
    }

    public async Task ExecuteCustomPrompt(ConversationContext context, ConversationReference reference,
    int promptId, Message message, string user, string replyToId, bool keepContext,
          CancellationToken cancellationToken)
    {
        var prompt = await _promptService.GetPromptAsync(promptId);
        await _proactiveMessageService.ExecuteCustomPromptAsync(reference, message.Content, prompt.Title, replyToId, user, cancellationToken);

        var currentConversation = await _conversationService.GetConversationByContextAsync(context);

        if (prompt.Assistant != null)
        {
            await _conversationService.ChangeConversationAssistantAsync(context, prompt.Assistant.Name);
        }

        if (prompt.Functions != null)
        {
            foreach (var function in prompt.Functions)
            {
                if (!currentConversation.AllFunctionNames.Any(t => t == function.Id))
                {
                    await _functionService.AddFunctionToConversationAsync(reference, function.Id);
                }
            }
        }

        if (context.ChatType != ChatType.personal)
        {
            await _messageService.CreateMessageAsync(message);
        }

        await ProcessMessageAsync(context, message, cancellationToken);

        if (!keepContext)
        {
            if (prompt.Assistant != null)
            {
                await _conversationService.ChangeConversationAssistantAsync(context, currentConversation.Assistant.Name);
            }

            if (prompt.Functions != null)
            {
                foreach (var function in prompt.Functions)
                {
                    if (!currentConversation.AllFunctionNames.Any(t => t == function.Id))
                    {
                        await _functionService.DeleteFunctionFromConversationAsync(reference, function.Id);
                    }

                }
            }

        }

    }

}