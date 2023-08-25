using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Cards;
using achappey.ChatGPTeams.Config;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Services;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace achappey.ChatGPTeams
{
    public class ChatGPTBot<T> : TeamsActivityHandler where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;
        private readonly IMapper _mapper;
        private readonly string _appId;

        private readonly ITokenService _tokenService;
        private readonly IChatGPTeamsBotChatService _chatGPTeamsBotChatService;
        private readonly IChatGPTeamsBotConfigService _chatGPTeamsBotConfigService;
        private readonly IPromptService _promptService;

        public ChatGPTBot(ConversationState conversationState,
                          UserState userState,
                          T dialog,
                          AppConfig config,
                          ILogger<ChatGPTBot<T>> logger,
                          IMapper mapper,
                          IPromptService promptService,
                          ITokenService tokenService,
                          IChatGPTeamsBotConfigService chatGPTeamsBotConfigService,
                          IChatGPTeamsBotChatService chatGPTeamsBotService)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
            _mapper = mapper;
            _promptService = promptService;
            _tokenService = tokenService;
            _appId = config.MicrosoftAppId;
            _chatGPTeamsBotChatService = chatGPTeamsBotService;
            _chatGPTeamsBotConfigService = chatGPTeamsBotConfigService;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {


            await base.OnTurnAsync(turnContext, cancellationToken);
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);


        }


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
                                                             CancellationToken cancellationToken)
        {
            await EnsureToken(turnContext, cancellationToken);

            var token = _tokenService.GetToken();

            if (token != null)
            {


                await _chatGPTeamsBotConfigService.EnsureConversation(turnContext.Activity.GetConversationReference());

                if (turnContext.Activity.Attachments != null)
                {
                    await _chatGPTeamsBotChatService.ProcessAttachmentsAsync(await turnContext.ToConversationContext(),
                                                                         turnContext.Activity.GetConversationReference(),
                                                                         turnContext.Activity.Attachments,
                                                                         cancellationToken);
                }

                if (!string.IsNullOrEmpty(turnContext.Activity.Text))
                {
                    var text = turnContext.Activity.Text.ToLowerInvariant().Trim();
                    string recipientNameLower = turnContext.Activity.Recipient.Name.ToLowerInvariant();

                    if (text == recipientNameLower
                        || text == $"<at>{recipientNameLower}</at>"
                        || text == $"<at>{recipientNameLower}</at> {recipientNameLower}")
                    {
                        await ShowMenuAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        if (text == "geschiedenis wissen")
                        {
                            await _chatGPTeamsBotConfigService.ClearHistoryAsync(await turnContext.ToConversationContext());
                        }
                        else
                        {
                            await _chatGPTeamsBotChatService.ProcessMessageAsync(await turnContext.ToConversationContext(), _mapper.Map<Message>(turnContext.Activity),
                                                                                                                            cancellationToken);
                        }
                    }
                }

                if (turnContext.Activity.Value != null)
                {
                    var data = turnContext.Activity.Value as JObject;
                    data = JObject.FromObject(data);

                    var actionType = data["ActionType"].Value<string>();
                    var context = await turnContext.ToConversationContext();

                    string titleFilter = null;
                    string ownerFilter = null;
                    Visibility? visiblityFilter = null;
                    if (data.ContainsKey("TitleFilter"))
                    {
                        titleFilter = data["TitleFilter"].Value<string>();
                    }

                    if (data.ContainsKey("VisibilityFilter"))
                    {
                        visiblityFilter = data["VisibilityFilter"].Value<string>().TextToVisibility();
                    }

                    if (data.ContainsKey("OwnerFilter"))
                    {
                        ownerFilter = data["OwnerFilter"].Value<string>();
                    }

                    switch (actionType)
                    {
                        case CardsConfigCommands.ExecutePromptAction:
                            var executePromptId = data["ExecutePromptId"].Value<string>();
                            var executePrompt = await _chatGPTeamsBotConfigService.GetPrompt(executePromptId);

                            var formCard = ChatCards.CreatePromptFormCard(executePrompt.Id, executePrompt.Content);
                            await turnContext.SendActivityAsync(MessageFactory.Attachment(formCard), cancellationToken: cancellationToken);
                            break;

                        case CardsConfigCommands.PromptFormAction:
                            var sourcePrompt = data["SourcePrompt"].Value<string>();
                            var promptId = data["PromptId"].Value<string>();

                            foreach (var property in data.Properties())
                            {
                                if (property.Name != "SourcePrompt" && property.Name != "ActionType" && property.Name != "PromptId")
                                {
                                    string placeholder = property.Name;
                                    string value = property.Value.Value<string>();

                                    // Replace placeholders with double curly brackets
                                    sourcePrompt = sourcePrompt.Replace($"{{{{{placeholder}}}}}", value);
                                }
                            }

                            // Repeat loop for single curly brackets
                            foreach (var property in data.Properties())
                            {
                                if (property.Name != "SourcePrompt" && property.Name != "ActionType" && property.Name != "PromptId")
                                {
                                    string placeholder = property.Name;
                                    string value = property.Value.Value<string>();

                                    // Replace placeholders with single curly brackets
                                    sourcePrompt = sourcePrompt.Replace($"{{{placeholder}}}", value);
                                }
                            }

                            var message = _mapper.Map<Message>(turnContext.Activity);

                            message.Content = sourcePrompt;

                            await _chatGPTeamsBotChatService.ExecuteCustomPrompt(await turnContext.ToConversationContext(),
                                                                             turnContext.Activity.GetConversationReference(),
                                                                             promptId,
                                                                             message,
                                                                             turnContext.Activity.From.Name,
                                                                             turnContext.Activity.ReplyToId,
                                                                             cancellationToken);


                            break;
                        case CardsConfigCommands.SavePromptAction:
                            var promptSaveId = data["ResourceId"].Value<string>();
                            var newContent = data["PromptContent"].Value<string>();
                            var newTitle = data["PromptTitle"].Value<string>();
                            var newPromptAssistant = data.ContainsKey("AssistantChoice") ? data["AssistantChoice"].Value<string>() : null;
                            var visiblity = data["VisibilityChoice"].Value<string>().TextToVisibility();
                            var functionChoices = data.ContainsKey("FunctionChoices") ? data["FunctionChoices"].Value<string>().ToFunctions() : null;


                            await _chatGPTeamsBotConfigService.UpdatePromptAsync(turnContext.Activity.GetConversationReference(),
                                                                                 promptSaveId,
                                                                                 newTitle,
                                                                                 newContent,
                                                                                 newPromptAssistant,
                                                                                 functionChoices,
                                                                                 visiblity,
                                                                                 turnContext.Activity.ReplyToId,
                                                                                 cancellationToken);

                            break;

                        case CardsConfigCommands.UpdateRoleAction:
                            var newAssistant = data["Role"].Value<string>();

                            await _chatGPTeamsBotConfigService.ChangeAssistantAsync(await turnContext.ToConversationContext(),
                                                                                    turnContext.Activity.GetConversationReference(),
                                                                                    newAssistant,
                                                                                    cancellationToken);
                            break;
                        case CardsConfigCommands.EditAssistantAction:
                            var newTemp = data[CardsConfigCommands.NewTemperature].Value<string>();
                            var newName = data[CardsConfigCommands.NewName].Value<string>();
                            var newAssistantfunctionChoices = data.ContainsKey("FunctionChoices") ? data["FunctionChoices"].Value<string>().ToFunctions() : new List<Function>();
                            var newAssistantvisiblity = data["VisibilityChoice"].Value<string>().TextToVisibility();
                            var newRole = data[CardsConfigCommands.NewRole].Value<string>();

                            await _chatGPTeamsBotConfigService.UpdateAssistantAsync(await turnContext.ToConversationContext(),
                                                                                    turnContext.Activity.GetConversationReference(),
                                                                                    newName,
                                                                                    newRole,
                                                                                    float.Parse(newTemp),
                                                                                    newAssistantfunctionChoices,
                                                                                    newAssistantvisiblity,
                                                                                    cancellationToken);
                            break;
                        case CardsConfigCommands.EditPromptAction:
                            var promptResourceId = data["ResourceId"].Value<string>();
                            var prompt = await _promptService.GetPromptAsync(promptResourceId);

                            await _chatGPTeamsBotConfigService.EditPromptAsync(turnContext.Activity.GetConversationReference(),
                                                                               prompt,
                                                                               null,
                                                                               cancellationToken);

                            break;
                        case CardsConfigCommands.CloneAssistantAction:
                            await _chatGPTeamsBotConfigService.CloneAssistantAsync(await turnContext.ToConversationContext(),
                                                                             turnContext.Activity.GetConversationReference(),
                                                                             cancellationToken);
                            break;
                        case CardsConfigCommands.AddFunctionAction:
                            var properties = data.Properties().Where(a => a.Name.StartsWith("Function"));
                            var values = properties.Select(a => a.Value.Value<string>()).Where(a => !string.IsNullOrEmpty(a));

                            await _chatGPTeamsBotConfigService.AddFunctionsAsync(await turnContext.ToConversationContext(),
                                                                           turnContext.Activity.GetConversationReference(),
                                                                           values,
                                                                           cancellationToken);
                            break;
                        case CardsConfigCommands.DeleteFunctionAction:
                            var function = data["FunctionName"].Value<string>();

                            await _chatGPTeamsBotConfigService.DeleteFunctionAsync(await turnContext.ToConversationContext(),
                                                                             turnContext.Activity.GetConversationReference(),
                                                                             function,
                                                                             cancellationToken);
                            break;
                        case CardsConfigCommands.CleanHistoryAction:
                            var keepMessages = data.ContainsKey("KeepMessages") ? data["KeepMessages"].Value<int?>() : null;

                            await _chatGPTeamsBotConfigService.ClearHistoryAsync(await turnContext.ToConversationContext());

                            await _chatGPTeamsBotConfigService.SelectPromptsAsync(context, turnContext.Activity.GetConversationReference(), 0, context.ReplyToId, titleFilter, ownerFilter, visiblityFilter, cancellationToken);
                            break;
                        case CardsConfigCommands.SelectFunctionsCommand:
                            await _chatGPTeamsBotConfigService.SelectFunctionsAsync(await turnContext.ToConversationContext(),
                                                                              turnContext.Activity.GetConversationReference(),
                                                                              null,
                                                                              cancellationToken);
                            break;
                        case CardsConfigCommands.SelectRoleCommand:
                            await _chatGPTeamsBotConfigService.SelectAssistantAsync(await turnContext.ToConversationContext(),
                                                                              turnContext.Activity.GetConversationReference(),
                                                                              null,
                                                                              cancellationToken);
                            break;
                        case CardsConfigCommands.SelectSourcesCommand:
                            await _chatGPTeamsBotConfigService.SelectResourcesAsync(await turnContext.ToConversationContext(),
                                                                              turnContext.Activity.GetConversationReference(),
                                                                              null,
                                                                              cancellationToken);

                            break;
                        case CardsConfigCommands.DeleteResourceAction:
                            var promptFormResourceId = data["ResourceId"].Value<string>();

                            await _chatGPTeamsBotConfigService.DeleteResourceAsync(await turnContext.ToConversationContext(),
                                                                             turnContext.Activity.GetConversationReference(),
                                                                             promptFormResourceId,
                                                                             cancellationToken);

                            break;
                        case CardsConfigCommands.SelectPromptCommand:
                            await _chatGPTeamsBotConfigService.SelectPromptsAsync(await turnContext.ToConversationContext(),
                                                                            turnContext.Activity.GetConversationReference(),
                                                                            0,
                                                                            null,
                                                                            null, null, null,
                                                                            cancellationToken);

                            break;
                        case CardsConfigCommands.DeletePromptAction:
                            await _chatGPTeamsBotConfigService.DeletePromptAsync(context,
                                                                           turnContext.Activity.GetConversationReference(),
                                                                           data["ResourceId"].Value<string>(),
                                                                           cancellationToken);
                            break;

                        case CardsConfigCommands.NextPromptPageAction:
                            var nextPage = data["NextPage"].Value<int>();

                            await _chatGPTeamsBotConfigService.SelectPromptsAsync(context,
                                                                           turnContext.Activity.GetConversationReference(),
                                                                           nextPage,
                                                                           context.ReplyToId,
                                                                            titleFilter, ownerFilter, visiblityFilter,
                                                                           cancellationToken);

                            break;
                        case CardsConfigCommands.PreviousPromptPageAction:
                            var prevPage = data["PreviousPage"].Value<int>();
                            await _chatGPTeamsBotConfigService.SelectPromptsAsync(context,
                                                                           turnContext.Activity.GetConversationReference(),
                                                                           prevPage,
                                                                           context.ReplyToId,
                                                                           titleFilter, ownerFilter, visiblityFilter,
                                                                           cancellationToken);

                            break;
                        case CardsConfigCommands.ApplyPromptFilter:
                            await _chatGPTeamsBotConfigService.SelectPromptsAsync(context,
                                                                           turnContext.Activity.GetConversationReference(),
                                                                           0,
                                                                           context.ReplyToId,
                                                                           titleFilter, ownerFilter, visiblityFilter,
                                                                           cancellationToken);

                            break;

                        case CardsConfigCommands.RemoveFunctionToPrompt:
                            var removeFunctionChoice = data["RemoveFunctionChoice"].Value<string>();
                            var promptRemoveId = data["PromptRemoveId"].Value<string>();

                            await _chatGPTeamsBotConfigService.DeleteFunctionFromPromptAsync(turnContext.Activity.GetConversationReference(),
                                                                                 promptRemoveId,
                                                                                 removeFunctionChoice,
                                                                                 turnContext.Activity.ReplyToId,
                                                                                 cancellationToken);


                            break;
                        case CardsConfigCommands.PromoteResource:
                            var resourceChoiceId = data["resourceChoice"].Value<string>();

                            await _chatGPTeamsBotConfigService.PromoteResourceToAssistantAsync(context,
                                                                                turnContext.Activity.GetConversationReference(),
                                                                                 resourceChoiceId,
                                                                                 turnContext.Activity.ReplyToId,
                                                                                 cancellationToken);


                            break;
                        default:
                            break;
                    }
                }
            }
        }

        protected override async Task OnTeamsMessageSoftDeleteAsync(ITurnContext<IMessageDeleteActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Conversation.ConversationType)
            {
                case "personal":
                    await EnsureToken(turnContext, cancellationToken);

                    await _chatGPTeamsBotChatService.DeleteMessageAsync(await turnContext.ToConversationContext(),
                                                                     turnContext.Activity.GetConversationReference(),
                                                                     turnContext.Activity.Id,
                                                                     cancellationToken);
                    break;
                default:
                    break;
            }
        }



        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(
            ITurnContext<IInvokeActivity> turnContext,
            MessagingExtensionAction action,
            CancellationToken cancellationToken)
        {
            /*        switch (action.CommandId)
                    {
                        case "webView":
                            return await CreateAdaptiveCardResponse(turnContext, action);

                    }*/

            return await Task.FromResult(new MessagingExtensionActionResponse());
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            await EnsureToken(turnContext, cancellationToken);

            var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

            switch (query.CommandId)
            {
                case "searchQuery":
                    var initialRun = query?.Parameters?[0]?.Name == "initialRun";
                    var prompts = initialRun ? await _chatGPTeamsBotConfigService.GetAllPrompts() : await _chatGPTeamsBotChatService.SearchPromptsAsync(text);

                    var attachments = prompts.Select(prompt =>
                    {
                        var previewCard = new ThumbnailCard
                        {
                            Title = prompt.Title,
                            Text = prompt.Content,
                            Tap = new CardAction
                            {
                                Type = "invoke",
                                Value = prompt
                            }
                        };

                        var attachment = new MessagingExtensionAttachment
                        {
                            ContentType = HeroCard.ContentType,
                            Content = new HeroCard
                            {
                                Title = prompt.Title,
                                Text = prompt.Content

                            },
                            Preview = previewCard.ToAttachment()
                        };

                        return attachment;
                    });

                    return new MessagingExtensionResponse
                    {
                        ComposeExtension = new MessagingExtensionResult
                        {
                            Type = "result",
                            AttachmentLayout = "list",
                            Attachments = attachments.ToList()
                        }
                    };
            }

            return new MessagingExtensionResponse();

        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {
            await EnsureToken(turnContext, cancellationToken);

            var selectedPrompt = query.ToObject<Prompt>();

            var formCard = ChatCards.CreatePromptFormCard(selectedPrompt.Id, selectedPrompt.Content);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(formCard), cancellationToken: cancellationToken);

            return new MessagingExtensionResponse
            {
            };
        }

        protected async Task EnsureToken(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

            var token = turnContext.GetLastResult();
            _tokenService.SetToken(token);
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            await EnsureToken(turnContext, cancellationToken);

            switch (action.CommandId)
            {
                case "saveMessage":
                    if (turnContext.Activity.Value is JObject messagePayload)
                    {
                        var bodyContent = messagePayload["messagePayload"]["body"]["content"].ToString();
                        var visibility = (Visibility)Enum.Parse(typeof(Visibility), messagePayload["data"]["visibility"].ToString());
                        var connectAiAssistant = bool.Parse(messagePayload["data"]["connectAiAssistant"].ToString()); // New line for connect AI assistant
                        var connectFunctions = bool.Parse(messagePayload["data"]["connectFunctions"].ToString()); // New line for connect functions
                        var title = messagePayload["data"]["title"].ToString();

                        var htmlTagPattern = "<.*?>";
                        var plainText = Regex.Replace(bodyContent, htmlTagPattern, string.Empty);

                        await _chatGPTeamsBotConfigService.SavePromptAsync(await turnContext.ToConversationContext(), title, plainText, visibility, connectAiAssistant, connectFunctions); // Adjusted parameters
                    }
                    break;

                case "webView":
                    return await Task.FromResult(new MessagingExtensionActionResponse()
                    {
                        ComposeExtension = new MessagingExtensionResult
                        {

                            Type = "message",
                            Text = "Hello"
                        }

                    });
            }

            return await Task.FromResult(new MessagingExtensionActionResponse());

        }


        protected async Task ShowMenuAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await _chatGPTeamsBotConfigService.ShowMenuAsync(await turnContext.ToConversationContext(),
                                                           turnContext.Activity.GetConversationReference(),
                                                           turnContext.Activity.Recipient.Name,
                                                           cancellationToken);
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Conversation.ConversationType)
            {
                case "personal":
                case "groupChat":

                    await EnsureToken(turnContext, cancellationToken);

                    switch (turnContext.Activity.Action)
                    {
                        case "add":
                            await _chatGPTeamsBotChatService.EnsureConversationByReferenceAsync(turnContext.Activity.GetConversationReference());
                            break;
                        case "remove":
                            await _chatGPTeamsBotChatService.DeleteConversationByReferenceAsync(turnContext.Activity.GetConversationReference());
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}