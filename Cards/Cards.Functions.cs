using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Config;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {
        public static Attachment CreateFunctionExecutedCard(string name, string incomingTextValue, string arguments)
        {
            // parse the JSON string to a dictionary
            var argumentDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(arguments);

            // create the facts from the dictionary
            List<AdaptiveFact> facts = argumentDict.Select(kvp => new AdaptiveFact(kvp.Key, kvp.Value)).ToList();

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
            new AdaptiveTextBlock
            {
                Text = name,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            },
        },
                Actions = {
            new AdaptiveShowCardAction
            {
                Title = CardsConfigText.ViewInputText,
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = {
                        new AdaptiveFactSet
                        {
                            Facts = facts
                        }
                    }
                }
            },
            new AdaptiveShowCardAction
            {
                Title = CardsConfigText.ViewResultText,
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = {
                        new AdaptiveTextBlock
                        {
                            Text = $"{incomingTextValue}",
                            Size = AdaptiveTextSize.Small,
                            Wrap = true
                        }
                    }
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


        public static Attachment CreateExecuteFunctionCard(string name, string arguments)
        {
            // parse the JSON string to a dictionary
            var argumentDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(arguments);

            // create the facts from the dictionary
            List<AdaptiveFact> facts = argumentDict.Select(kvp => new AdaptiveFact(kvp.Key, kvp.Value)).ToList();

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
            new AdaptiveTextBlock
            {
                Text = name,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            },
            new AdaptiveFactSet
            {
                Facts = facts
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

