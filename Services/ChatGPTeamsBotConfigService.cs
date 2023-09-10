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
                           int promptId,
                           string title,
                           string category,
                           string content,
                           int? assistant,
                           IEnumerable<Function> functions,
                           Visibility visibilty,
                           string replyToId,
                           CancellationToken cancellationToken);

    Task DeleteFunctionFromPromptAsync(ConversationReference reference,
                           int promptId,
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
                           int promptId,
                           CancellationToken cancellationToken);
    Task DeleteResourceAsync(ConversationContext context,
                             ConversationReference reference,
                             int resourceId,
                             CancellationToken cancellationToken);

    Task<IEnumerable<Prompt>> GetAllPrompts();

    Task<Prompt> GetPrompt(int id);

    Task ClearHistoryAsync(ConversationContext context);

    Task EnsureConversation(ConversationContext context);

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
        var conversation = await _conversationService.GetConversationAsync(context.Id);

       // var conversationId = await _conversationService.GetConversationIdByContextAsync(context);
      //  var assistant = await _assistantService.GetAssistantByConversationTitle(reference.Conversation.Id);
      //  var functions = await _functionService.GetFunctionsByConversation(reference.Conversation.Id);
       // var resources = await _resourceService.GetResourcesByConversationTitleAsync(reference.Conversation.Id);
        var messages = await _messageService.GetByConversationAsync(context, context.Id);
        await _proactiveMessageService.ShowMenuAsync(reference,
                                                     appName,
                                                     conversation.Assistant.Name,
                                                     conversation.AllFunctionNames.Count(),
                                                     conversation.AllResources.Count(),
                                                     messages.Count(),
                                                     cancellationToken);
    }

    public async Task EnsureConversation(ConversationContext context)
    {
     //   if (context.ChatType == ChatType.channel)
     //   {
            await _conversationService.EnsureConversationAsync(context.Id, context.TeamsId, context.ChannelId);
      //  }
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
            .Where(f => !conversation.AllFunctionNames.Any(cf => cf == f.Id))
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

        await _proactiveMessageService.SelectPromptsAsync(reference, prompts, replyToId,
        context.UserDisplayName, skip, titleFilter, categoryFilter, ownerFilter, visibilityFilter, cancellationToken);
    }

    public async Task SelectResourcesAsync(ConversationContext context, ConversationReference reference, string replyToId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByContextAsync(context);
        var isOwner = conversation.Assistant.Owner != null && conversation.Assistant.Owner.Id == reference.User.AadObjectId;
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

    public async Task ChangeAssistantAsync(ConversationContext context, ConversationReference reference, 
            string assistantName, CancellationToken cancellationToken)
    {
        var assistant = await _assistantService.GetAssistantByName(assistantName);
        await _conversationService.ChangeConversationAssistantAsync(context, assistant.Name);

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

        if (conversation.Assistant.Owner != null && conversation.Assistant.Owner.Id == currentUser.Id)
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
       int promptId,
       CancellationToken cancellationToken)
    {
        await _promptService.DeletePromptAsync(promptId);

        await SelectPromptsAsync(context, reference, 0, context.ReplyToId, null, null, null, null, cancellationToken);
    }

    public async Task DeleteResourceAsync(ConversationContext context,
          ConversationReference reference,
          int resourceId,
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
            var conversation = await _conversationService.GetConversationAsync(context.Id);
            newPrompt.Functions =conversation.Functions;
        }

        await _promptService.CreatePromptAsync(newPrompt);
    }

    public async Task UpdatePromptAsync(ConversationReference reference, int promptId, string title, string category,
            string content, int? assistant, IEnumerable<Function> functions,
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

        prompt.Assistant = assistant.HasValue ? await _assistantService.GetAssistant(assistant.Value) : null;

        await _promptService.UpdatePromptAsync(prompt);

        prompt = await _promptService.GetPromptAsync(prompt.Id);

        await _proactiveMessageService.PromptUpdatedAsync(reference, prompt, replyToId, cancellationToken);
    }

    public async Task EditPromptAsync(ConversationReference reference, Prompt prompt, string replyToId, CancellationToken cancellationToken)
    {
        var currentItem = await _promptService.GetPromptAsync(prompt.Id);
        var assistants = await _assistantService.GetMyAssistants();
        var functions = await _functionService.GetAllFunctionsAsync();
        var categories = await _promptService.GetCategories();

        await _proactiveMessageService.EditPromptAsync(reference, currentItem, assistants,
                functions, categories, replyToId, cancellationToken);
    }

    public async Task DeleteFunctionFromPromptAsync(ConversationReference reference, int promptId,
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

    public async Task<Prompt> GetPrompt(int id)
    {
        return await _promptService.GetPromptAsync(id);
    }
}


