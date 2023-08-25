// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Cards;
using achappey.ChatGPTeams.Services;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams
{
    // This bot is derived (view DialogBot<T>) from the TeamsACtivityHandler class currently included as part of this sample.

    public class ChatGPTeamsBot<T> : ChatGPTBot<T> where T : Dialog
    {

        public ChatGPTeamsBot(ConversationState conversationState, UserState userState, AppConfig appConfig,
        T dialog, ILogger<ChatGPTBot<T>> logger, IPromptService promptService,
        IMapper mapper, ITokenService tokenService, IChatGPTeamsBotChatService chatGPTeamsBotChatService,  IChatGPTeamsBotConfigService chatGPTeamsBotConfigService)
            : base(conversationState, userState, dialog, appConfig, logger,  mapper, promptService, tokenService, chatGPTeamsBotConfigService, chatGPTeamsBotChatService)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
          // await EnsureToken(turnContext, cancellationToken);
            
            await ShowMenuAsync(turnContext, cancellationToken);
            //await turnContext.SendActivityAsync(MessageFactory.Attachment(ChatCards.CreateHeroCard(turnContext.Activity.Recipient.Name)), cancellationToken);
        }
 
        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.
  
            // Run the Dialog with the new Invoke Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }


    }
}