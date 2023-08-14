using System.Threading;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Dialogs;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class EventBasedDialog : LogoutDialog
{
    protected readonly ILogger Logger;
    protected readonly IMapper _mapper;
  
    public EventBasedDialog(IConfiguration configuration, ILogger<EventBasedDialog> logger, IMapper mapper)
        : base(nameof(EventBasedDialog), configuration["ConnectionName"])
    {
        Logger = logger;
        _mapper = mapper;

        AddDialog(new OAuthPrompt(
            nameof(OAuthPrompt),
            new OAuthPromptSettings
            {
                ConnectionName = ConnectionName,
                Text = $"Log in met je {ConnectionName} account",
                Title = "Inloggen",
                Timeout = 300000,
                EndOnInvalidMessage = true
            }));

        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            PromptStepAsync,
            LoginStepAsync,
        }));

        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
    }

    private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get the token from the previous step. Note that we could also have gotten the
        // token directly from the prompt itself. There is an example of this in the next method.
        var tokenResponse = (TokenResponse)stepContext.Result;
        
        if (tokenResponse?.Token == null)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Inloggen mislukt."), cancellationToken);
        }

        return await stepContext.EndDialogAsync(tokenResponse?.Token, cancellationToken: cancellationToken);
    }


}
