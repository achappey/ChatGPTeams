using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Config;
using achappey.ChatGPTeams.Models;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {
        public static Attachment CreateFunctionsInfoCardWithDelete(IEnumerable<Function> roleFunctions, IEnumerable<Function> conversationFunctions, IEnumerable<Function> availableFunctions)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
            new AdaptiveContainer
            {
                Items = {
                    new AdaptiveTextBlock
                    {
                        Text = CardsConfigText.ChooseYourChatOptionsText,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    }
                }
            }
        }
            };

            // Group functions by Publisher
            var functionsGroupedByPublisher = availableFunctions.GroupBy(f => f.Publisher);

            foreach (var publisherGroup in functionsGroupedByPublisher)
            {
                var publisherCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

                publisherCard.Body.Add(
                   new AdaptiveTextBlock
                   {
                       Text = CardsConfigText.AiSelectOptionsText,
                       Weight = AdaptiveTextWeight.Bolder,
                       Size = AdaptiveTextSize.Medium
                   });

                // Group functions of this publisher by Category
                var functionsGroupedByCategory = publisherGroup.GroupBy(f => f.Category).OrderBy(a => a.Key);

                foreach (var categoryGroup in functionsGroupedByCategory)
                {
                    // Create choices for current group
                    var functionChoices = categoryGroup.Select(f => new AdaptiveChoice { Title = f.Title, Value = f.Name }).ToList();

                    // Add a new dropdown for each group
                    publisherCard.Body.Add(new AdaptiveChoiceSetInput()
                    {
                        Id = $"Function_{publisherGroup.Key}_{categoryGroup.Key}",
                        Choices = functionChoices,
                        Placeholder = $"{categoryGroup.Key}",
                        Style = AdaptiveChoiceInputStyle.Compact
                    });
                }

                publisherCard.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = CardsConfigText.OkText,
                    Data = new { ActionType = CardsConfigCommands.AddFunctionAction }
                });



                // Add a new show card action for each publisher
                card.Actions.Add(new AdaptiveShowCardAction
                {
                    Title = $"{publisherGroup.Key}",
                    Card = publisherCard
                });
            }

            card.Body.AddRange(CreateFunctionSection(roleFunctions, CardsConfigText.AiAssistantOptionsText));
            card.Body.AddRange(CreateFunctionSection(conversationFunctions, CardsConfigText.MyOptionsText));

               card.Body.Add(new AdaptiveTextBlock
                {
                    Text = "Selecteer een categorie om functies toe te voegen.",
                    Weight = AdaptiveTextWeight.Default,
                    Wrap = true,
                    Size = AdaptiveTextSize.Default
                });


            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }


        private static List<AdaptiveElement> CreateFunctionSection(IEnumerable<Function> functions, string header)
        {
            var section = new List<AdaptiveElement>();

            functions = functions.OrderBy(a => a.Category);

            if (functions.Any())
            {
                section.Add(new AdaptiveTextBlock
                {
                    Text = header,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium
                });

                foreach (var function in functions)
                {
                    section.Add(new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveTextBlock
                            {
                                Text = $"{function.Category}: {function.Title}",
                                Wrap = true
                            }
                        },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveActionSet
                            {
                                Actions = {
                                    new AdaptiveSubmitAction
                                    {
                                        Title = CardsConfigText.RemoveText,
                                        Data = new { ActionType = CardsConfigCommands.DeleteFunctionAction, FunctionName = function.Name }
                                    }
                                }
                            }
                        },
                        Width = AdaptiveColumnWidth.Auto
                    }
                }
                    });
                }
            }

            return section;
        }
    }
}

