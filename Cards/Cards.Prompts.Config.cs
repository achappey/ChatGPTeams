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
                string categoryFilter = null,
                string ownerFilter = null,
                Visibility? visiblityFilter = null)
        {
            var titles = prompts.Select(a => a.Title).GroupBy(a => a).Select(a => a.Key);
            var owners = prompts.Select(a => a.Owner.DisplayName).GroupBy(a => a).Select(a => a.Key);
            var visibilityOptions = Enum.GetValues(typeof(Visibility)).Cast<Visibility>().Select(a => new AdaptiveChoice { Title = a.ToText(), Value = a.ToText() }).ToList();

            // Counting active filters
            int activeFilters = (titleFilter != null ? 1 : 0) + (ownerFilter != null ? 1 : 0) + (visiblityFilter != null ? 1 : 0) + (categoryFilter != null ? 1 : 0);

            if (!string.IsNullOrEmpty(titleFilter))
            {
                prompts = prompts.Where(a => a.Title != null && a.Title.Contains(titleFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                prompts = prompts.Where(a => a.Category != null && a.Category == categoryFilter);
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

            // List of potential category names
            List<string> categories = prompts.Select(t => t.Category).Distinct().ToList();

            // Create the ChoiceSet with the selected categories

            var categoryChoiceSet = new AdaptiveChoiceSetInput
            {
                Id = "CategorySelection",
                Placeholder = "Selecteer een categorie",
                Value = categoryFilter, // default value
                Choices = categories.Select(category => new AdaptiveChoice { Title = category, Value = category }).ToList(),
                Style = AdaptiveChoiceInputStyle.Compact
            };

            card.Actions.Add(new AdaptiveShowCardAction
            {
                Title = $"Meer filters ({activeFilters})",
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body =
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = "Filter opties:",
                                    Weight = AdaptiveTextWeight.Bolder
                                },
              /*                  new AdaptiveChoiceSetInput
                                {
                                    Id = "TitleFilter",
                                    Placeholder = CardsConfigText.SelectName,
                                    Value = titleFilter,
                                    Choices = titles.Select(a => new AdaptiveChoice { Title = a, Value = a }).ToList(),
                                    Style = AdaptiveChoiceInputStyle.Compact
                                },*/
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

            // Eerste Rij - Categorie
            // Eerste Rij - Tekstinvoer
            var textInputAction1 = new AdaptiveSubmitAction
            {
                Title = "Zoeken",
                Id = "TitleFilterButton",
                Data = new { ActionType = CardsConfigCommands.ApplyPromptFilter }
            };

            var textInput = new AdaptiveTextInput
            {
                Id = "TitleFilter",
                Value = titleFilter,
                Placeholder = "Zoek in de naam of dialoog",
            };

            var firstRow = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
    {
        new AdaptiveColumn // Voor de tekstinvoer
        {
            Items = { textInput },
            Width = AdaptiveColumnWidth.Stretch
        },
        new AdaptiveColumn // Voor de eerste knop
        {
            Items = { new AdaptiveActionSet { Actions = new List<AdaptiveAction> { textInputAction1 } } },
            Width = AdaptiveColumnWidth.Auto
        }
    }
            };

            // Tweede Rij - Een andere categorie
            var selectCategoryAction2 = new AdaptiveSubmitAction
            {
                Title = "Filter categorie",
                Id = "CategorySelection",
                
                Data = new { ActionType = CardsConfigCommands.ApplyPromptFilter }
            };

            var secondRow = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
    {
        new AdaptiveColumn // Voor de tweede dropdown
        {
            Items = { categoryChoiceSet  },
            Width = AdaptiveColumnWidth.Stretch
        },
        new AdaptiveColumn // Voor de tweede knop
        {
            Items = { new AdaptiveActionSet { Actions = new List<AdaptiveAction> { selectCategoryAction2 } } },
            Width = AdaptiveColumnWidth.Auto
        }
    }
            };

            // Samenvoegen
            var allRows = new List<AdaptiveElement> { firstRow, secondRow };



            /*
            // Eerste rij: Categorie Invoer en Knop
            var categoryAction = new AdaptiveSubmitAction
            {
                Title = "Filter categorie",
                Id = "CategoryAction",
                Data = new { ActionType = "FilterCategory" }
            };

            // Tweede rij: Zoek Invoer en Knop
            var searchAction = new AdaptiveSubmitAction
            {
                Title = "Zoeken",
                Id = "SearchAction",
                Data = new { ActionType = "SearchAction" }
            };

            var updatedCategoryColumnSet = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    // Eerste rij
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveTextInput { Id = "CategoryInput", Placeholder = "Categorie..." },
                            new AdaptiveActionSet { Actions = new List<AdaptiveAction> { categoryAction } }
                        },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    // Tweede rij
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveTextInput { Id = "SearchInput", Placeholder = "Zoek..." },
                            new AdaptiveActionSet { Actions = new List<AdaptiveAction> { searchAction } }
                        },
                        Width = AdaptiveColumnWidth.Stretch
                    }
                }
            };
            // Eerste rij
            var firstRow = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn // Voor het eerste dropdown
                    {
                        Items = { categoryChoiceSet },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn // Voor de eerste knop
                    {
                        Items = { new AdaptiveActionSet { Actions = new List<AdaptiveAction> { selectCategoryAction } } },
                        Width = AdaptiveColumnWidth.Auto
                    }
                }
            };

            // Tweede rij
            var secondRow = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn // Voor het tweede dropdown
                    {
                        Items = { anotherCategoryChoiceSet },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn // Voor de tweede knop
                    {
                        Items = { new AdaptiveActionSet { Actions = new List<AdaptiveAction> { anotherSelectCategoryAction } } },
                        Width = AdaptiveColumnWidth.Auto
                    }
                }
            };

            // Voeg rijen toe aan kaartlichaam
            card.Body.Add(firstRow);
            card.Body.Add(secondRow);
            */

            /*

                        var searchInput = new AdaptiveTextInput
                        {
                            Id = "SearchFilter",
                            Placeholder = "Zoek...",
                            Value = "" // Standaardwaarde
                        };

                        var searchAction = new AdaptiveSubmitAction
                        {
                            Title = "Zoeken",
                            Id = "SearchAction",
                            Data = new { ActionType = "SearchAction" } // Gebruik deze ActionType om te weten wanneer er gezocht wordt
                        };



                        var selectCategoryAction = new AdaptiveSubmitAction
                        {
                            Title = "Filter categorie",
                            Id = "CategoryFilter",
                            Data = new { ActionType = CardsConfigCommands.ApplyPromptFilter } // Use this ActionType in your bot logic to know when a category has been selected
                        };


            var updatedCategoryColumnSet = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    // Eerste rij
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveTextInput { Id = "CategoryInput", Placeholder = "Categorie..." },
                            new AdaptiveActionSet { Actions = new List<AdaptiveAction> { categoryAction } }
                        },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    // Tweede rij
                    new AdaptiveColumn
                    {
                        Items = {
                            new AdaptiveTextInput { Id = "SearchInput", Placeholder = "Zoek..." },
                            new AdaptiveActionSet { Actions = new List<AdaptiveAction> { searchAction } }
                        },
                        Width = AdaptiveColumnWidth.Stretch
                    }
                }
            };
                        // Update de bestaande categoryColumnSet
                        var updatedCategoryColumnSet = new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn // Voor de dropdown
                    {
                        Items = { categoryChoiceSet },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn // Voor de zoekfunctie
                    {
                        Items = { searchInput },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn // Voor de knoppen
                    {
                        Items = {
                            new AdaptiveActionSet { Actions = new List<AdaptiveAction> { selectCategoryAction, searchAction } }
                        },
                        Width = AdaptiveColumnWidth.Auto
                    }
                }
                        };



                        // Create a ColumnSet to align the dropdown next to the submit button
                        var categoryColumnSet = new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn // For the dropdown
                    {
                        Items = { categoryChoiceSet },
                        Width = AdaptiveColumnWidth.Stretch
                    },
                    new AdaptiveColumn // For the submit button
                    {
                        Items = { new AdaptiveActionSet { Actions = new List<AdaptiveAction> { selectCategoryAction } } },
                        Width = AdaptiveColumnWidth.Auto
                    }
                }
                        };
            */


            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "Dialogen" + $" ({page + 1} van {totalPages})",
                Size = AdaptiveTextSize.Default,
                Weight = AdaptiveTextWeight.Bolder
            });




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
                    actions.Add(editAction);
                }

                var titleAndContentColumn = new AdaptiveColumn
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Items = {
            new AdaptiveTextBlock
            {
                Text = $"{prompt.Title}",
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium,
            },
            new AdaptiveTextBlock
            {
                Text = $"{prompt.Content}",
                Size = AdaptiveTextSize.Small
            }
        }
                };

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
            titleAndContentColumn,   // This column contains the title and content
            actionsColumn            // This column contains the action buttons
        },
                    Separator = true,               // This adds the separator
                    Spacing = AdaptiveSpacing.Default // Optional, adjust for desired spacing
                });



            }

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = "Filters",
                Size = AdaptiveTextSize.Default,
                Weight = AdaptiveTextWeight.Bolder
            });

            //card.Body.Add(updatedCategoryColumnSet);
            card.Body.AddRange(allRows);
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

        }

    }
}
