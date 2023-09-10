using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Dialogs;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class EventBasedDialog : LogoutDialog
{
    protected readonly ILogger Logger;
    protected readonly IMapper _mapper;
    //protected readonly ITokenService _tokenService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EventBasedDialog(IConfiguration configuration, ILogger<EventBasedDialog> logger, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
        : base(nameof(EventBasedDialog), configuration["ConnectionName"])
    {
        Logger = logger;
        _mapper = mapper;

        _serviceScopeFactory = serviceScopeFactory;

        AddDialog(new OAuthPrompt(
             "Graph",
            new OAuthPromptSettings
            {
                ConnectionName = ConnectionName,
                Text = $"Log in bij {ConnectionName}",
                Title = "Inloggen",
                Timeout = 300000,
                EndOnInvalidMessage = false
            }));


         AddDialog(new OAuthPrompt(
                             "Dataverse",
                             new OAuthPromptSettings
                             {
                                 ConnectionName = "Simplicate 365",
                                 Text = $"Log in bij Simplicate 365",
                                 Title = "Inloggen",
                                 Timeout = 300000,
                                 EndOnInvalidMessage = true
                             }));

        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            PromptStep1Async,
            LoginStep1Async,
            PromptStep2Async,
            LoginStep2Async,
        }));


        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> PromptStep1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        stepContext.Values["tokens"] = new Dictionary<string, string>();
        
        return await stepContext.BeginDialogAsync("Graph", null, cancellationToken);
    }
    private async Task<DialogTurnResult> PromptStep2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var result = await stepContext.BeginDialogAsync("Dataverse", null, cancellationToken);

        return result;
    }

    private async Task<DialogTurnResult> LoginStep1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get the token from the previous step. Note that we could also have gotten the
        // token directly from the prompt itself. There is an example of this in the next method.
        var tokenResponse = (TokenResponse)stepContext.Result;

        if (tokenResponse?.Token == null)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Inloggen mislukt."), cancellationToken);
        }
        else
        {
            var tokens = (Dictionary<string, string>)stepContext.Values["tokens"];
        tokens["Graph"] = tokenResponse.Token;

/*            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                tokenService.SetToken(tokenResponse?.Token);
            }*/
            //        var tokenService = (ITokenService)_serviceProvider.GetService(typeof(ITokenService));


        }

        //return await stepContext.EndDialogAsync(tokenResponse?.Token, cancellationToken: cancellationToken);
        return await stepContext.NextAsync(stepContext.Values["tokens"], cancellationToken: cancellationToken);
        //return await stepContext.ContinueDialogAsync(cancellationToken);
    }

    private async Task<DialogTurnResult> LoginStep2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get the token from the previous step. Note that we could also have gotten the
        // token directly from the prompt itself. There is an example of this in the next method.
        var tokenResponse = (TokenResponse)stepContext.Result;

        if (tokenResponse?.Token == null)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Inloggen mislukt."), cancellationToken);
        }
        else
        {
              var tokens = (Dictionary<string, string>)stepContext.Values["tokens"];
        tokens["Dataverse"] = tokenResponse.Token;

        //      using (var scope = _serviceScopeFactory.CreateScope())
        //    {
          //      var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            //    tokenService.SetDataverseToken(tokenResponse?.Token);
           // }
            //var tokenService = (ITokenService)_serviceProvider.GetService(typeof(ITokenService));

            //tokenService.SetDataverseToken(tokenResponse?.Token);
            //   _tokenService.SetDataverseToken(tokenResponse?.Token);
        }

        return await stepContext.EndDialogAsync(stepContext.Values["tokens"], cancellationToken: cancellationToken);
    }

}
