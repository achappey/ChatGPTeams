using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using achappey.ChatGPTeams.Cards;
using achappey.ChatGPTeams.Config;

namespace achappey.ChatGPTeams.Services;


public interface IProactiveMessageService
{
    Task<string> SendMessageAsync(ConversationReference conversationReference, string message, CancellationToken cancellationToken);

    Task SelectAssistantAsync(ConversationReference conversationReference,
        Conversation conversation,
        IEnumerable<Assistant> allAssistants,
        IEnumerable<Function> allFunctions,
        string replyToId,
        CancellationToken cancellationToken);

    Task SelectPromptsAsync(ConversationReference conversationReference,
        IEnumerable<Prompt> prompts,
        string replyToId,
        string currentUser,
        int page,
        string titleFilter,
        string ownerFilter,
        int messageCount,
        Visibility? visibilityFilter,
        CancellationToken cancellationToken);

    Task SelectFunctionsAsync(ConversationReference conversationReference,
        IEnumerable<Function> assistantFunctions,
        IEnumerable<Function> conversationFunctions,
        IEnumerable<Function> availableFunctions,
        string replyToId,
        CancellationToken cancellationToken);

    Task SelectResourcesAsync(ConversationReference conversationReference,
        bool isAssistantOwner,
        int messageCount,
        IEnumerable<Resource> assistantResources,
        IEnumerable<Resource> conversationResources,
        string replyToId,
        CancellationToken cancellationToken);

    Task<string> ExecuteFunctionAsync(ConversationReference conversationReference,
        Function function,
        FunctionCall functionCall,
        CancellationToken cancellationToken);

    Task FunctionExecutedAsync(ConversationReference conversationReference,
        Function function,
        FunctionCall functionCall,
        string result,
        string replyId,
        CancellationToken cancellationToken);

    Task<string> ImportResourceAsync(ConversationReference conversationReference,
       Resource resource,
       CancellationToken cancellationToken);

    Task ImportResourceFinishedAsync(ConversationReference conversationReference,
        Resource resource,
        int lineCount,
        string cardId,
        CancellationToken cancellationToken);

    Task ExecuteCustomPromptAsync(ConversationReference conversationReference,
      string sourcePrompt,
      string replyToId,
      string user,
      CancellationToken cancellationToken);

    Task PromptUpdatedAsync(ConversationReference conversationReference,
       Prompt prompt,
       string replyToId,
       CancellationToken cancellationToken);

    Task EditPromptAsync(ConversationReference conversationReference, Prompt prompt, IEnumerable<Assistant> assistants,
            IEnumerable<Function> functions, string replyToId, CancellationToken cancellationToken);
    Task ShowMenuAsync(ConversationReference conversationReference,
        string appName,
        string assistantName,
        int functionCount,
        int resourceCount,
        int messageCount,
        CancellationToken cancellationToken);
}

public class ProactiveMessageService : IProactiveMessageService
{
    private readonly CloudAdapter _adapter;
    private readonly string _appId;

    public ProactiveMessageService(CloudAdapter adapter, AppConfig config)
    {
        _adapter = adapter;
        _appId = config.MicrosoftAppId;
    }

    public async Task<string> SendMessageAsync(ConversationReference conversationReference, string message, CancellationToken cancellationToken)
    {
        string activityId = null;

        await _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var response = await turnContext.SendActivityAsync(message);
            activityId = response.Id;
        }, cancellationToken);

        return activityId;
    }

    public Task ShowMenuAsync(ConversationReference conversationReference,
        string appName,
        string assistantName,
        int functionCount,
        int resourceCount,
        int messageCount,
        CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
              {
                  var card = MessageFactory.Attachment(ChatCards.CreateHeroCard(appName, assistantName, functionCount, resourceCount, messageCount));

                  await turnContext.SendActivityAsync(card, cancellationToken);

              }, cancellationToken);
    }


    public async Task<string> ExecuteFunctionAsync(ConversationReference conversationReference,
        Function function,
        FunctionCall functionCall,
        CancellationToken cancellationToken)
    {
        string activityId = null;
        await _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var card = MessageFactory.Attachment(ChatCards.CreateExecuteFunctionCard(function.Title, functionCall.Arguments));

                   var response = await turnContext.SendActivityAsync(card, cancellationToken);

                   activityId = response.Id;
               }, cancellationToken);
        return activityId;
    }

    public async Task FunctionExecutedAsync(ConversationReference conversationReference,
        Function function,
        FunctionCall functionCall,
        string result,
        string replyId,
        CancellationToken cancellationToken)
    {
        await _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var card = MessageFactory.Attachment(ChatCards.CreateFunctionExecutedCard(function.Title, result));
                   card.Id = replyId;

                   await turnContext.UpdateActivityAsync(card, cancellationToken);

               }, cancellationToken);
    }

    public async Task<string> ImportResourceAsync(ConversationReference conversationReference,
           Resource resource,
           CancellationToken cancellationToken)
    {
        string activityId = null;

        await _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var card = MessageFactory.Attachment(ChatCards.CreateImportDocumentCard(CardsConfigText.ImportResourceText, resource.Name));

                   var response = await turnContext.SendActivityAsync(card, cancellationToken);
                   activityId = response.Id;

               }, cancellationToken);

        return activityId;
    }

    public async Task ImportResourceFinishedAsync(ConversationReference conversationReference,
          Resource resource,
          int lineCount,
          string cardId,
          CancellationToken cancellationToken)
    {

        await _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var card = MessageFactory.Attachment(ChatCards.CreateImportDocumentCard(CardsConfigText.ImportResourceCompletedText, resource.Name));
                   card.Id = cardId;

                   await turnContext.UpdateActivityAsync(card, cancellationToken);

               }, cancellationToken);
    }

    public Task ExecuteCustomPromptAsync(ConversationReference conversationReference,
       string sourcePrompt,
       string replyToId,
       string user,
       CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var promptExecuteCard = MessageFactory.Attachment(ChatCards.CreateExecutePromptCard(sourcePrompt, user));
                   promptExecuteCard.Id = replyToId;

                   await turnContext.UpdateActivityAsync(promptExecuteCard, cancellationToken);

               }, cancellationToken);
    }

    public Task PromptUpdatedAsync(ConversationReference conversationReference,
       Prompt prompt,
       string replyToId,
       CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
               {
                   var promptExecuteCard = MessageFactory.Attachment(ChatCards.CreatePromptEditedCard(prompt, conversationReference.User.Name));
                   promptExecuteCard.Id = replyToId;

                   await turnContext.UpdateActivityAsync(promptExecuteCard, cancellationToken);

               }, cancellationToken);
    }

    public Task SelectAssistantAsync(ConversationReference conversationReference,
        Conversation currentConversation,
        IEnumerable<Assistant> allAssistants,
        IEnumerable<Function> allFunctions,
        string replyToId,
        CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var card = MessageFactory.Attachment(ChatCards.CreateRoleInfoCard(currentConversation.Assistant.Name,
            currentConversation.Assistant.Prompt,
            currentConversation.Temperature,
            currentConversation.Assistant.Model,
            currentConversation.Assistant.Visibility,
            currentConversation.Assistant.Functions,
            currentConversation.Assistant.Owners.Select(a => a.DisplayName),
            allAssistants.Select(a => a.Name).ToArray(),
            allFunctions,
            currentConversation.Assistant.Owners.Any(a => a.DisplayName == turnContext.Activity.From.Name)));

            if (!string.IsNullOrEmpty(replyToId))
            {
                card.Id = replyToId;
                await turnContext.UpdateActivityAsync(card, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(card, cancellationToken);
            }

        }, cancellationToken);
    }

    public Task SelectFunctionsAsync(ConversationReference conversationReference,
        IEnumerable<Function> assistantFunctions,
        IEnumerable<Function> conversationFunctions,
        IEnumerable<Function> availableFunctions,
      string replyToId,
        CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var card = MessageFactory.Attachment(ChatCards.CreateFunctionsInfoCardWithDelete(assistantFunctions,
            conversationFunctions,
            availableFunctions));

            if (!string.IsNullOrEmpty(replyToId))
            {
                card.Id = replyToId;
                await turnContext.UpdateActivityAsync(card, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(card, cancellationToken);
            }

        }, cancellationToken);
    }

    public Task SelectResourcesAsync(ConversationReference conversationReference,
        bool isAssistantOwner,
        int messageCount,
        IEnumerable<Resource> assistantResources,
        IEnumerable<Resource> conversationResources,
        string replyToId,
        CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var card = MessageFactory.Attachment(ChatCards.CreateResourcesInfoCardWithDelete(isAssistantOwner,
            assistantResources, conversationResources));

            if (!string.IsNullOrEmpty(replyToId))
            {
                card.Id = replyToId;
                await turnContext.UpdateActivityAsync(card, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(card, cancellationToken);
            }

        }, cancellationToken);
    }

    public Task SelectPromptsAsync(ConversationReference conversationReference,
        IEnumerable<Prompt> prompts,
        string replyToId,
        string currentUser,
        int page,
        string titleFilter,
        string ownerFilter,
        int messageCount,
        Visibility? visibilityFilter,
       CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var card = MessageFactory.Attachment(ChatCards.CreatePromptInfoCard(prompts, currentUser, messageCount, page, titleFilter, ownerFilter, visibilityFilter));
            if (!string.IsNullOrEmpty(replyToId))
            {
                card.Id = replyToId;
                await turnContext.UpdateActivityAsync(card, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(card, cancellationToken);
            }

        }, cancellationToken);
    }

    public Task EditPromptAsync(ConversationReference conversationReference, Prompt prompt, IEnumerable<Assistant> assistants,
            IEnumerable<Function> functions, string replyToId, CancellationToken cancellationToken)
    {
        return _adapter.ContinueConversationAsync(_appId, conversationReference, async (turnContext, cancellationToken) =>
        {
            var card = MessageFactory.Attachment(ChatCards.CreateEditPromptCard(prompt, assistants, functions));

            if (!string.IsNullOrEmpty(replyToId))
            {
                card.Id = replyToId;
                await turnContext.UpdateActivityAsync(card, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(card, cancellationToken);
            }

            //await turnContext.SendActivityAsync(card, cancellationToken);

        }, cancellationToken);
    }
}