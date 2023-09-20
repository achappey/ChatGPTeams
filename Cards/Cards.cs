using achappey.ChatGPTeams.Config;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {

        public static Attachment CreateErrorCard(string errorMessage)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                    new AdaptiveTextBlock
                    {
                        Text = CardsConfigText.AiError,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Default
                    },
                    new AdaptiveTextBlock
                    {
                        Text = errorMessage,
                        Wrap = true,
                        Size = AdaptiveTextSize.Medium,
                        Weight = AdaptiveTextWeight.Default
                    }
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static Attachment CreateMenuCard(string appName, string assistantName, float creativity, int functionCount, int resourceCount, int messageCount)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                    new AdaptiveTextBlock
                    {
                        Text = $"{CardsConfigText.WelcomeText} {appName}",
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                },
                Actions = {
                    new AdaptiveSubmitAction
                    {
                        Title = $"{CardsConfigText.SelectYourAiAssistantText} ({assistantName}, {creativity})",
                        Data = new { ActionType = CardsConfigCommands.SelectRoleCommand }
                    },
                    new AdaptiveSubmitAction
                    {
                        Title = $"{CardsConfigText.ChooseYourChatOptionsText} ({functionCount})",
                        Data = new { ActionType = CardsConfigCommands.SelectFunctionsCommand }
                    },
                    new AdaptiveSubmitAction
                    {
                        Title = $"{CardsConfigText.ChooseYourDataSourcesText} ({resourceCount})",
                        Data = new { ActionType = CardsConfigCommands.SelectSourcesCommand }
                },
                new AdaptiveSubmitAction
                {
                    Title =  $"{CardsConfigText.SearchDialogueInspirationsText} ({messageCount})",
                    Data = new { ActionType = CardsConfigCommands.SelectPromptCommand }
                }
            }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }
    }
}

