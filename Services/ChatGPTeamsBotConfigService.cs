using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using achappey.ChatGPTeams.Models;
using System.Linq;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Services;

public interface IChatGPTeamsBotConfigService
{
    Task CloneAssistantAsync(ConversationContext context,
                             ConversationReference reference,
                             CancellationToken cancellationToken);

    Task EditPromptAsync(
                             ConversationReference reference,
                             Prompt prompt,
                             string replyToId,
                             CancellationToken cancellationToken);
    Task SelectAssistantAsync(ConversationContext context,
                              ConversationReference reference,
                              string replyToId,
                              CancellationToken cancellationToken);

    Task AddFunctionsAsync(ConversationContext context,
                           ConversationReference reference,
                           IEnumerable<string> functionNames,
                           CancellationToken cancellationToken);

    Task SelectFunctionsAsync(ConversationContext context,
                              ConversationReference reference,
                              string replyToId,
                              CancellationToken cancellationToken);
    Task SelectResourcesAsync(ConversationContext context,
                              ConversationReference reference,
                              string replyToId,
                              CancellationToken cancellationToken);
    Task DeleteFunctionAsync(ConversationContext context,
                             ConversationReference reference,
                             string functionName,
                             CancellationToken cancellationToken);
    Task ChangeAssistantAsync(ConversationContext context,
                              ConversationReference reference,
                              string assistantName,
                              CancellationToken cancellationToken);
    Task UpdateAssistantAsync(ConversationContext context,
                              ConversationReference reference,
                              string assistantName,
                              string assistantRole,
                              float temperature,
                              IEnumerable<Function> functions,
                              Visibility visibility,
                              CancellationToken cancellationToken);
    Task SavePromptAsync(ConversationContext context,
        string title, string prompt, Visibility visibility, bool connectAssistant, bool connectFunctions);

    Task UpdatePromptAsync(ConversationReference reference,
                           string promptId,
                           string title,
                           string category,
                           string content,
                           string assistant,
                           IEnumerable<Function> functions,
                           Visibility visibilty,
                           string replyToId,
                           CancellationToken cancellationToken);

    Task DeleteFunctionFromPromptAsync(ConversationReference reference,
                           string promptId,
                           string functionId,
                           string replyToId,
                           CancellationToken cancellationToken);

    Task SelectPromptsAsync(ConversationContext context,
                            ConversationReference reference,
                            int skip,
                            string replyToId,
                            string titleFilter,
                            string categoryFilter,
                            string ownerFilter,
                            Visibility? visibilityFilter,
                            CancellationToken cancellationToken);
    Task DeletePromptAsync(ConversationContext context,
                           ConversationReference reference,
                           string promptId,
                           CancellationToken cancellationToken);
    Task DeleteResourceAsync(ConversationContext context,
                             ConversationReference reference,
                             string resourceId,
                             CancellationToken cancellationToken);

    Task<IEnumerable<Prompt>> GetAllPrompts();

    Task<Prompt> GetPrompt(string id);

    Task ClearHistoryAsync(ConversationContext context);

    Task EnsureConversation(ConversationReference reference);

    Task ShowMenuAsync(ConversationContext context,
                                           ConversationReference reference,
                                           string appName,
                                           CancellationToken cancellationToken);

    Task PromoteResourceToAssistantAsync(ConversationContext context, ConversationReference reference, string resourceId,
       string replyToId,
        CancellationToken cancellationToken);

}

public class ChatGPTeamsBotConfigService : IChatGPTeamsBotConfigService
{
    private readonly IAssistantService _assistantService;
    private readonly IConversationService _conversationService;
    private readonly IResourceService _resourceService;
    private readonly IDepartmentService _departmentService;
    private readonly IFunctionService _functionService;
    private readonly IPromptService _promptService;
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;

    private readonly IProactiveMessageService _proactiveMessageService;
    private readonly IMapper _mapper;


    public ChatGPTeamsBotConfigService(IAssistantService assistantService,
                                 IConversationService conversationService,
                                 IUserService userService,
                                 IMapper mapper,
                                 IMessageService messageService,
                                 IPromptService promptService,
                                 IDepartmentService departmentService,
                                 IProactiveMessageService proactiveMessageService,
                                 IFunctionService functionService,
                                 IResourceService resourceService)
    {
        _assistantService = assistantService;
        _conversationService = conversationService;
        _functionService = functionService;
        _promptService = promptService;
        _messageService = messageService;
        _resourceService = resourceService;
        _userService = userService;
        _departmentService = departmentService;
        _mapper = mapper;
        _proactiveMessageService = proactiveMessageService;
    }

    public async Task ShowMenuAsync(ConversationContext context,
                                              ConversationReference reference,
                                              string appName,
                                              CancellationToken cancellationToken)
    {
        var conversationId = await _conversationService.GetConversationIdByContextAsync(context);
        var assistant = await _assistantService.GetAssistantByConversationTitle(reference.Conversation.Id);
        var functions = await _functionService.GetFunctionsByConversation(reference.Conversation.Id);
        var resources = await _resourceService.GetResourcesByConversationTitleAsync(reference.Conversation.Id);
        var messages = await _messageService.GetByConversationAsync(context, conversationId);
        await _proactiveMessageService.ShowMenuAsync(reference,
                                                     appName,
                                                     assistant.Name,
                                                     functions.Count(),
                                                     resources.Count(),
                                                     messages.Count(),
                                                     cancellationToken);
    }

    public async Task EnsureConversation(ConversationReference reference)
    {
        if (reference.Conversation.ConversationType == "channel")
        {
            await _conversationService.EnsureConversationByReferenceAsync(reference);
        }
    }


    public async Task SelectFunctionsAsync(ConversationContext context,
                                           ConversationReference reference,
                                           string replyToId,
                                           CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context); // Retrieve the conversation
        var allFunctions = await _functionService.GetAllFunctionsAsync(); // Get all functions

        // Determine available functions by filtering out ones that are already assigned
        var availableFunctions = allFunctions
            .Where(f => !conversation.AllFunctionNames.Any(cf => cf == f.Name))
            .ToList();

        // Identify assistant-specific and conversation-specific functions
        var assistantFunctions = allFunctions.Where(f => conversation.Assistant.Functions.Any(af => af.Id == f.Id));
        var conversationFunctions = allFunctions.Where(f => conversation.Functions.Any(cf => cf.Id == f.Id));

        // Send the selection to the proactive message service
        await _proactiveMessageService.SelectFunctionsAsync(reference,
                                                            assistantFunctions,
                                                            conversationFunctions,
                                                            availableFunctions,
                                                            replyToId,
                                                            cancellationToken);
    }

    public async Task<IEnumerable<Prompt>> GetAllPrompts()
    {
        return await _promptService.GetMyPromptsAsync();
    }


    public async Task SelectAssistantAsync(ConversationContext context, ConversationReference reference, string replyToId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        var assistants = await _assistantService.GetMyAssistants();
        var functions = await _functionService.GetAllFunctionsAsync();

        await _proactiveMessageService.SelectAssistantAsync(reference, conversation, assistants, functions, replyToId, cancellationToken);
    }

    public async Task SelectPromptsAsync(ConversationContext context, ConversationReference reference, int skip, string replyToId, string titleFilter,
                           string categoryFilter, string ownerFilter, Visibility? visibilityFilter, CancellationToken cancellationToken)
    {
        var prompts = await _promptService.GetMyPromptsAsync();
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        var messages = await _messageService.GetByConversationAsync(context, conversation.Id);

        await _proactiveMessageService.SelectPromptsAsync(reference, prompts, replyToId,
        context.UserDisplayName, skip, titleFilter, categoryFilter, ownerFilter, messages.Count(), visibilityFilter, cancellationToken);
    }

    public async Task SelectResourcesAsync(ConversationContext context, ConversationReference reference, string replyToId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        var isOwner = conversation.Assistant.Owners.Any(a => a.DisplayName == reference.User.Name);
        var messages = await _messageService.GetByConversationAsync(context, conversation.Id);

        await _proactiveMessageService.SelectResourcesAsync(reference, isOwner, messages.Count(),
                conversation.Assistant.Resources, conversation.Resources, replyToId, cancellationToken);
    }

    public async Task DeleteFunctionAsync(ConversationContext context, ConversationReference reference, string functionName, CancellationToken cancellationToken)
    {
        await _functionService.DeleteFunctionFromConversationAsync(reference, functionName);
        await SelectFunctionsAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task CloneAssistantAsync(ConversationContext context,
                                          ConversationReference reference,
                                          CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        conversation.Assistant = await _assistantService.CloneAssistantAsync(conversation.Assistant.Id);

        await _conversationService.UpdateConversationAsync(conversation);

        await SelectAssistantAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task AddFunctionsAsync(ConversationContext context,
                                        ConversationReference reference,
                                        IEnumerable<string> functionNames,
                                        CancellationToken cancellationToken)
    {
        foreach (var functionName in functionNames)
        {
            await _functionService.AddFunctionToConversationAsync(reference, functionName);
        }

        await SelectFunctionsAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task ChangeAssistantAsync(ConversationContext context, ConversationReference reference, string assistantName, CancellationToken cancellationToken)
    {
        var assistant = await _assistantService.GetAssistantByName(assistantName);
        await _conversationService.ChangeConversationAssistantAsync(context, assistant.Id);

        await SelectAssistantAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task UpdateAssistantAsync(ConversationContext context,
        ConversationReference reference,
        string assistantName,
        string assistantRole,
        float temperature,
        IEnumerable<Function> functions,
        Visibility visibility,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        var currentUser = await _userService.GetCurrentUser();

        if (conversation.Assistant.Owners.Any(y => y.Id == currentUser.Id))
        {
            var assistant = await _assistantService.GetAssistant(conversation.Assistant.Id);
            assistant.Prompt = assistantRole;
            assistant.Name = assistantName;
            assistant.Functions = functions;
            assistant.Temperature = temperature;
            assistant.Visibility = visibility;
            assistant.Department = assistant.Visibility == Visibility.Department ?
                currentUser.Department : null;

            await _assistantService.UpdateAssistantAsync(assistant);
        }

        conversation.Temperature = temperature;

        await _conversationService.UpdateConversationAsync(conversation);

        await SelectAssistantAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task DeletePromptAsync(ConversationContext context,
       ConversationReference reference,
       string promptId,
       CancellationToken cancellationToken)
    {
        await _promptService.DeletePromptAsync(promptId);

        await SelectPromptsAsync(context, reference, 0, context.ReplyToId, null, null, null, null, cancellationToken);
    }

    public async Task DeleteResourceAsync(ConversationContext context,
          ConversationReference reference,
          string resourceId,
          CancellationToken cancellationToken)
    {
        await _resourceService.DeleteResourceAsync(resourceId);

        await SelectResourcesAsync(context, reference, context.ReplyToId, cancellationToken);
    }

    public async Task ClearHistoryAsync(ConversationContext context)
    {
        await _conversationService.ClearConversationHistoryAsync(context, null);

    }

    public async Task SavePromptAsync(ConversationContext context,
        string title, string prompt, Visibility visibility, bool connectAssistant, bool connectFunctions)
    {
        var newPrompt = new Prompt()
        {
            Content = prompt,
            Title = title,
            Visibility = visibility,
            Owner = await _userService.GetCurrentUser()
        };

        if (visibility == Visibility.Department)
        {
            newPrompt.Department = await _departmentService.GetDepartmentByNameAsync(newPrompt.Owner.Department.Name);
        }

        if (connectAssistant)
        {
               newPrompt.Assistant = await _assistantService.GetAssistantByConversationTitle(context.Id);
        }

        if (connectFunctions)
        {
               newPrompt.Functions = await _functionService.GetFunctionsByConversation(context.Id);
        }

        await _promptService.CreatePromptAsync(newPrompt);
    }

    public async Task UpdatePromptAsync(ConversationReference reference, string promptId, string title, string category,
    string content, string assistant, IEnumerable<Function> functions,
    Visibility visibilty, string replyToId, CancellationToken cancellationToken)
    {
        var prompt = await _promptService.GetPromptAsync(promptId);
        prompt.Title = title;
        prompt.Content = content;
        prompt.Visibility = visibilty;
        prompt.Category = category;
        prompt.Functions = functions;

        if (prompt.Visibility == Visibility.Department)
        {
            var user = await _userService.GetCurrentUser();

            prompt.Department = await _departmentService.GetDepartmentByNameAsync(user.Department.Name);
        }
        else
        {
            prompt.Department = null;
        }

        prompt.Assistant = !string.IsNullOrEmpty(assistant) ? await _assistantService.GetAssistant(assistant) : null;

        await _promptService.UpdatePromptAsync(prompt);

        prompt = await _promptService.GetPromptAsync(prompt.Id);

        await _proactiveMessageService.PromptUpdatedAsync(reference, prompt, replyToId, cancellationToken);
    }

    public async Task EditPromptAsync(ConversationReference reference, Prompt prompt, string replyToId, CancellationToken cancellationToken)
    {
        var assistants = await _assistantService.GetMyAssistants();
        var functions = await _functionService.GetAllFunctionsAsync();
        var categories = await _promptService.GetCategories();

        await _proactiveMessageService.EditPromptAsync(reference, prompt, assistants,
                functions, categories, replyToId, cancellationToken);
    }

    public async Task DeleteFunctionFromPromptAsync(ConversationReference reference, string promptId,
        string functionId,
        string replyToId,
         CancellationToken cancellationToken)
    {
        var prompt = await _promptService.GetPromptAsync(promptId);

        prompt.Functions = prompt.Functions.Where(a => a.Id != functionId);

        await _promptService.UpdatePromptAsync(prompt);

        var updatedPrompt = await _promptService.GetPromptAsync(promptId);

        await EditPromptAsync(reference, updatedPrompt, replyToId, cancellationToken);

    }

    public async Task PromoteResourceToAssistantAsync(ConversationContext context, ConversationReference reference, string resourceId,
       string replyToId,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetResource(resourceId);
        resource.Assistant = await _assistantService.GetAssistantByConversationTitle(reference.Conversation.Id); ;
        resource.Conversation = null;

        await _resourceService.UpdateResourceAsync(resource);

        await SelectResourcesAsync(context, reference, replyToId, cancellationToken);
    }

    public async Task<Prompt> GetPrompt(string id)
    {
        return await _promptService.GetPromptAsync(id);
    }
}


