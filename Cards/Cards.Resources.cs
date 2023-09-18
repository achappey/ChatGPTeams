using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Config;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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


        public static Attachment CreateContextQueryCard(string contextQueryJson)
        {
            var contextQueryList = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(contextQueryJson);
            var columnSets = new List<AdaptiveElement>();

            foreach (var item in contextQueryList)
            {
                var containerItems = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = $"{item.Url}",
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            },
        };

                foreach (var textItem in item.Text)
                {
                    containerItems.Add(new AdaptiveTextBlock
                    {
                        Text = $"{textItem}",
                        Wrap = true,
                        Separator = true,
                    });
                }

                columnSets.Add(new AdaptiveContainer
                {
                    Items = containerItems
                });
            }

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body =
        {
            new AdaptiveTextBlock
            {
                Text = "Bronteksten",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            }

        },
                Actions =
        {
            new AdaptiveShowCardAction
            {
                Title = CardsConfigText.ShowSourcesText,
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                     Body = columnSets
                }
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

