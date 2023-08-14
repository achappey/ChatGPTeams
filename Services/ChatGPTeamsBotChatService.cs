using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using achappey.ChatGPTeams.Models;
using System.Linq;
using Microsoft.Bot.Schema;
using achappey.ChatGPTeams.Extensions;

namespace achappey.ChatGPTeams.Services;

public interface IChatGPTeamsBotChatService
{
    Task OnChatReactionAdded(ConversationContext context,
                             ConversationReference reference,
                             string reaction,
                             CancellationToken cancellationToken);
    Task OnChatReactionRemoved(ConversationContext context,
                               ConversationReference reference,
                               string reaction,
                               CancellationToken cancellationToken);
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
    Task EnsureConversationByReferenceAsync(ConversationReference reference);
    Task DeleteConversationByReferenceAsync(ConversationReference reference);
    Task ExecuteCustomPrompt(ConversationContext context, ConversationReference reference, string promptId, Message message, string user, string replyToId,
          CancellationToken cancellationToken);
      

}

public class ChatGPTeamsBotChatService : IChatGPTeamsBotChatService
{
    private readonly IAssistantService _assistantService;
    private readonly IConversationService _conversationService;
    private readonly IResourceService _resourceService;
    private readonly IFunctionService _functionService;
    private readonly IPromptService _promptService;
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;
    private readonly IFunctionExecutionService _functionExecutionService;

    private readonly IProactiveMessageService _proactiveMessageService;
    private readonly IMapper _mapper;


    public ChatGPTeamsBotChatService(IAssistantService assistantService,
                                 IConversationService conversationService,
                                 IUserService userService,
                                 IMapper mapper,
                                 IMessageService messageService,
                                 IPromptService promptService,
                                 IProactiveMessageService proactiveMessageService,
                                 IChatService chatService,
                                 IFunctionService functionService,
                                 IResourceService resourceService,
                                 IFunctionExecutionService functionExecutionService)
    {
        _assistantService = assistantService;
        _conversationService = conversationService;
        _functionService = functionService;
        _functionExecutionService = functionExecutionService;
        _promptService = promptService;
        _messageService = messageService;
        _chatService = chatService;
        _resourceService = resourceService;
        _userService = userService;
        _mapper = mapper;
        _proactiveMessageService = proactiveMessageService;
    }

    private async Task ExecuteFunctionAsync(ConversationContext context,
                                            ConversationReference reference,
                                           // Conversation conversation,
                                            FunctionCall functionCall,
                                            CancellationToken cancellationToken)
    {
        var function = await _functionService.GetFunctionByNameAsync(functionCall.Name); // Retrieve function details

        string executionCardId = null;

        if (function != null)
        {
            executionCardId = await _proactiveMessageService.ExecuteFunctionAsync(reference, function, functionCall, cancellationToken); // Execute function
        }

        var result = await _functionExecutionService.ExecuteFunction(reference, functionCall);

        if (executionCardId != null && result != null)
        {
            await _proactiveMessageService.FunctionExecutedAsync(reference, function, functionCall, result, executionCardId, cancellationToken);
        }

        await ProcessChatAsync(context, reference, cancellationToken); // Continue processing the chat conversation
    }

    public async Task OnChatReactionRemoved(ConversationContext context,
                                            ConversationReference reference,
                                            string reaction,
                                            CancellationToken cancellationToken)
    {
        await _messageService.DeleteReactionFromMessage(reaction, reference);

    }

    public async Task OnChatReactionAdded(ConversationContext context,
                                          ConversationReference reference,
                                          string reaction,
                                          CancellationToken cancellationToken)
    {
        await _messageService.AddReactionToMessage(new Reaction() { Title = reaction }, reference);
        var user = await _userService.GetByAadObjectId(reference.User.AadObjectId);

        var messageId = await _messageService.CreateMessageAsync(new Message()
        {
            Role = Role.user,
            Reference = reference,
            Content = user.DisplayName + " heeft gereageerd met " + reaction,
            ConversationId = reference.Conversation.Id,
        });

        var result = await _chatService.SendRequest(context);

        await _proactiveMessageService.SendMessageAsync(reference, result.Content, cancellationToken);

        await _messageService.DeleteMessageById(messageId);

    }

  
    public async Task ProcessMessageAsync(ConversationContext context, Message message, CancellationToken cancellationToken)
    {
        if (message.Reference.Conversation.ConversationType == "personal")
        {
            await _messageService.CreateMessageAsync(message);
        }

        await ProcessChatAsync(context, message.Reference, cancellationToken);
    }

    private async Task ProcessChatAsync(ConversationContext context,
                                        ConversationReference reference,
                                        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context); // Retrieve the conversation using the context
        var result = await _chatService.SendRequest(context); // Send request using the chat service

        if (!string.IsNullOrEmpty(result.Content)) // Check if the result content is not empty
        {
            result.TeamsId = await _proactiveMessageService.SendMessageAsync(reference, result.Content, cancellationToken); // Send proactive message
        }

        if (reference.Conversation.ConversationType == "personal" && !string.IsNullOrEmpty(result.Content)) // Check if conversation is personal and result content is not empty
        {
            await _messageService.CreateMessageAsync(result); // Create a message for personal conversation
        }

        if (result.FunctionCall != null) // Check if there's a function call in the result
        {


            await ExecuteFunctionAsync(context, reference, result.FunctionCall, cancellationToken);
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
                                .Select(g => g.First());

        foreach (var item in items)
        {
            //  if (item.Name.StartsWith("https://") && item.Name.IsSharePointUrl()) // Check if the resource is a SharePoint URL
            //  {
            //    item.Name = await _resourceService.GetFileName(item); // Get the file name if it is a SharePoint URL
            // }

            var cardId = await _proactiveMessageService.ImportResourceAsync(reference, item, cancellationToken); // Import the resource
            var lineCount = await _resourceService.ImportResourceAsync(reference, item); // Get the line count of the resource

            await _proactiveMessageService.ImportResourceFinishedAsync(reference, item, lineCount, cardId, cancellationToken); // Signal the completion of the import
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

    public async Task EnsureConversationByReferenceAsync(ConversationReference reference)
    {
        await _conversationService.EnsureConversationByReferenceAsync(reference);
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


    public async Task ExecuteCustomPrompt(ConversationContext context, ConversationReference reference, string promptId, Message message, string user, string replyToId,
          CancellationToken cancellationToken)
    {
        await _proactiveMessageService.ExecuteCustomPromptAsync(reference, message.Content, replyToId, user, cancellationToken);

        var prompt = await _promptService.GetPromptAsync(promptId);

        var currentConversation = await _conversationService.GetConversationByContextAsync(context);

        if (prompt.Assistant != null)
        {
            await _conversationService.ChangeConversationAssistantAsync(context, prompt.Assistant.Id);
        }

        if (prompt.Functions != null)
        {
            foreach (var function in prompt.Functions)
            {
                if (!currentConversation.AllFunctionNames.Any(t => t == function.Name))
                {
                    await _functionService.AddFunctionToConversationAsync(reference, function.Name);
                }
            }
        }

        if (context.ChatType != ChatType.personal)
        {
            await _messageService.CreateMessageAsync(message);
        }

        await ProcessMessageAsync(context, message, cancellationToken);

        if (prompt.Assistant != null)
        {
            await _conversationService.ChangeConversationAssistantAsync(context, currentConversation.Assistant.Id);
        }

        if (prompt.Functions != null)
        {
            foreach (var function in prompt.Functions)
            {
                if (!currentConversation.AllFunctionNames.Any(t => t == function.Name))
                {
                    await _functionService.DeleteFunctionFromConversationAsync(reference, function.Name);
                }

            }
        }

    }

}