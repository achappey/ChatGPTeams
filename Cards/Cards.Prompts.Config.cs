using System;
using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Config;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {
        public static int PromptPageSize = 3;

        public static Attachment CreatePromptInfoCard(IEnumerable<Prompt> prompts, string currentUser, int messageCount, int page = 0,
                string titleFilter = null,
                string ownerFilter = null,
                Visibility? visiblityFilter = null)
        {
            var titles = prompts.Select(a => a.Title).GroupBy(a => a).Select(a => a.Key);
            var owners = prompts.Select(a => a.Owner.DisplayName).GroupBy(a => a).Select(a => a.Key);
            var visibilityOptions = Enum.GetValues(typeof(Visibility)).Cast<Visibility>().Select(a => new AdaptiveChoice { Title = a.ToText(), Value = a.ToText() }).ToList();

            // Counting active filters
            int activeFilters = (titleFilter != null ? 1 : 0) + (ownerFilter != null ? 1 : 0) + (visiblityFilter != null ? 1 : 0);

            if (!string.IsNullOrEmpty(titleFilter))
            {
                prompts = prompts.Where(a => a.Title != null && a.Title.Contains(titleFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (visiblityFilter.HasValue)
            {
                prompts = prompts.Where(a => a.Visibility == visiblityFilter.Value);
            }


            if (!string.IsNullOrEmpty(ownerFilter))
            {
                prompts = prompts.Where(a => a.Owner != null && a.Owner.DisplayName.Contains(ownerFilter, StringComparison.OrdinalIgnoreCase));
            }

            var currentPrompts = prompts.Skip(PromptPageSize * page).Take(PromptPageSize);
            var totalPrompts = prompts.Count();
            var totalPages = (totalPrompts + PromptPageSize - 1) / PromptPageSize;

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body =
                {
                    new AdaptiveTextBlock
                    {
                        Text = CardsConfigText.ChooseYourPromptsText,
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder
                    }
                },
                Actions =
                {


                }
            };


            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = $"{CardsConfigText.CleanHistoryText} ({messageCount})",
                Tooltip = "Wis de geschiedenis van je huidige conversatie",
                Data = new
                {
                    ActionType = CardsConfigCommands.CleanHistoryAction
                }
            });

            card.Actions.Add(new AdaptiveShowCardAction
            {
                Title = $"Filters ({activeFilters})",
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body =
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = "Filter opties:",
                                    Weight = AdaptiveTextWeight.Bolder
                                },
                                new AdaptiveChoiceSetInput
                                {
                                    Id = "TitleFilter",
                                    Placeholder = CardsConfigText.SelectName,
                                    Value = titleFilter,
                                    Choices = titles.Select(a => new AdaptiveChoice { Title = a, Value = a }).ToList(),
                                    Style = AdaptiveChoiceInputStyle.Compact
                                },
                                new AdaptiveChoiceSetInput
                                {
                                    Id = "OwnerFilter",
                                    Value = ownerFilter,
                                    Placeholder = CardsConfigText.SelectOwner,
                                        Choices = owners.Select(a => new AdaptiveChoice { Title = a, Value = a }).ToList(),
                                    Style = AdaptiveChoiceInputStyle.Compact
                                },
                                new AdaptiveChoiceSetInput
                                {
                                    Id = "VisibilityFilter",
                                    Value = visiblityFilter?.ToText(),
                                    Placeholder = CardsConfigText.SelectVisibilityText,
                                    Choices = visibilityOptions,
                                    Style = AdaptiveChoiceInputStyle.Compact
                                }
                            },
                    Actions =
                            {
                                new AdaptiveSubmitAction
                                {
                                    Title =  CardsConfigText.OkText,
                                    Data = new { ActionType = CardsConfigCommands.ApplyPromptFilter }
                                }
                            }
                }
            });

            // Add "Previous Page" button if there is a previous page
            if (page > 0)
            {
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = $"{CardsConfigText.PrevPageText} ",
                    Tooltip = "Vorige pagina",
                    Data = new
                    {
                        ActionType = CardsConfigCommands.PreviousPromptPageAction,
                        PreviousPage = page - 1,

                        TitleFilter = titleFilter,
                        OwnerFilter = ownerFilter
                    }
                });
            }

            // Add "Next Page" button if there is a next page
            if (page < totalPages - 1)
            {
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = $"{CardsConfigText.NextPageText}",
                    Tooltip = "Volgende pagina",
                    Data = new
                    {
                        ActionType = CardsConfigCommands.NextPage,
                        NextPage = page + 1,
                        TitleFilter = titleFilter,
                        OwnerFilter = ownerFilter
                    }
                });
            }

            foreach (var prompt in currentPrompts)
            {
                var actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = CardsConfigText.ExecuteText,
                        Data = new { ActionType = CardsConfigCommands.ExecutePromptAction, ExecutePromptId = prompt.Id }
                    }
                };

                if (prompt.Owner.DisplayName == currentUser)
                {


                    var editAction = new AdaptiveSubmitAction
                    {
                        Title = "Wijzigen",
                        Data = new { ActionType = CardsConfigCommands.EditPromptAction, ResourceId = prompt.Id }
                    };
                    actions.Add(editAction); // Added the editAction to the actions list

                }

                // Moved the creation of the editAction outside of the column set

                card.Body.Add(new AdaptiveColumnSet
                {
                    Style = AdaptiveContainerStyle.Emphasis,
                    Columns = new List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Items = {
                                new AdaptiveTextBlock
                                {
                                    Text = $"{prompt.Title}",
                                    Weight = AdaptiveTextWeight.Bolder,
                                    Size = AdaptiveTextSize.Medium,
                                    Wrap = true
                                }
                            },
                            Width = AdaptiveColumnWidth.Stretch
                        }
                    }
                });

                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = $"{prompt.Content}",
                    Wrap = true,
                    Size = AdaptiveTextSize.Small
                });

                var actionsColumn = new AdaptiveColumn { Width = AdaptiveColumnWidth.Auto };
                foreach (var action in actions)
                {
                    actionsColumn.Items.Add(new AdaptiveActionSet { Actions = new List<AdaptiveAction> { action } });
                }

                var visibilityText = prompt.Visibility == Visibility.Department ? prompt.Department.Name : prompt.Visibility == Visibility.Owner ?
                prompt.Owner.DisplayName : CardsConfigText.EveryoneText;

                card.Body.Add(new AdaptiveColumnSet
                {
                    Columns = new List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Items = {
                                new AdaptiveFactSet
                                {
                                    Separator = true,
                                    Spacing = AdaptiveSpacing.Padding,
                                    Facts = new List<AdaptiveFact>
                                    {
                                        new AdaptiveFact { Title = "Eigenaar", Value = prompt.Owner?.DisplayName },
                                        new AdaptiveFact { Title = "Zichtbaarheid", Value = visibilityText },
                                        new AdaptiveFact { Title = "AI-assistent", Value = prompt.Assistant?.Name },
                                        new AdaptiveFact { Title = "Functies", Value = string.Join(", ", prompt.Functions?.Select(a => a.Title)) }
                                    }
                                }
                            },
                            Width = AdaptiveColumnWidth.Stretch
                        },
                        actionsColumn
                    }
                });
            }

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }
    }
}

