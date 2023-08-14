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

        public static Attachment CreateResourcesInfoCardWithDelete(bool isAssistantOwner,
        IEnumerable<Resource> roleResources,
        IEnumerable<Resource> conversationResources)
        {



            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
            new AdaptiveContainer
            {
                Items = {
                    new AdaptiveTextBlock
                    {
                        Text = CardsConfigText.ChooseYourDataSourcesText,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    }
                }
            }
        },
                Actions = {
            }
            };

            if (!roleResources.Any() && !conversationResources.Any())
            {
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = "Je hebt nog geen chatbronnen toegevoegd. Stuur een bericht met een link om een bron toe te voegen.",
                    Weight = AdaptiveTextWeight.Default,
                    Wrap = true,
                    Size = AdaptiveTextSize.Default
                });

            }
            else
            {
                card.Body.AddRange(CreateResourceSection(isAssistantOwner, roleResources, CardsConfigText.TagResourcesText, true));
                card.Body.AddRange(CreateResourceSection(isAssistantOwner, conversationResources, CardsConfigText.MyResourcesText, false));

            }




            if (isAssistantOwner && conversationResources.Count() > 0)
            {
                var choices = conversationResources.Select(r => new AdaptiveChoice { Title = r.Name, Value = r.Id.ToString() });
                card.Actions.Add(new AdaptiveShowCardAction
                {
                    Title = CardsConfigText.AiPromoteResourceText,
                    Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                    {
                        Body =
                        {
                    new AdaptiveChoiceSetInput
                    {
                        Id = "resourceChoice",
                        Choices = choices.ToList(),
                        Placeholder = CardsConfigText.SelectResourceText,
                        Label =  CardsConfigText.PromoteResourceDescriptionText,
                        Style = AdaptiveChoiceInputStyle.Compact
                    }
                },
                        Actions = {
                    new AdaptiveSubmitAction
                    {
                        Title = CardsConfigText.OkText,
                        Data = new { ActionType = CardsConfigCommands.PromoteResource }
                    }
                }
                    }
                });
            }
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        private static List<AdaptiveElement> CreateResourceSection(bool isAssistantOwner, IEnumerable<Resource> resources, string header, bool isRoleResources)
        {
            var section = new List<AdaptiveElement>();

            resources = resources.OrderBy(a => a.Name);

            if (resources.Any())
            {

                section.Add(new AdaptiveTextBlock
                {
                    Text = header,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium
                });

                foreach (var function in resources)
                {
                    var columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    Items = {
                        new AdaptiveTextBlock
                        {
                            Text = $"{function.Name}",
                            Wrap = true
                        }
                    },
                    Width = AdaptiveColumnWidth.Stretch
                }
            };
                    if (!isRoleResources || (isAssistantOwner && isRoleResources))
                    {
                        columns.Add(new AdaptiveColumn
                        {
                            Items = {
                        new AdaptiveActionSet
                        {
                            Actions = {
                                new AdaptiveSubmitAction
                                {
                                    Title = CardsConfigText.RemoveText,
                                    Data = new { ActionType = CardsConfigCommands.DeleteResourceAction, ResourceId = function.Id }
                                }
                            }
                        }
                    },
                            Width = AdaptiveColumnWidth.Auto
                        });
                    }




                    section.Add(new AdaptiveColumnSet { Columns = columns });
                }
            }

            return section;
        }

    }
}

