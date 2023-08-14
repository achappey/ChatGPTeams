using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {
        public static Attachment CreateImportDocumentCard(string cardTitle, string filename)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                new AdaptiveTextBlock
                    {
                        Text = cardTitle,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock
                    {
                        Text = filename,
                        Wrap = true,
                        Size = AdaptiveTextSize.Medium,
                        Weight = AdaptiveTextWeight.Default
                    },
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

