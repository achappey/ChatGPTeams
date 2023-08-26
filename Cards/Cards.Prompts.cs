using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using achappey.ChatGPTeams.Config;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Cards
{
    public static partial class ChatCards
    {


        public static Attachment CreateExecutePromptCard(string prompt, string user)
        {
            // parse the JSON string to a dictionary

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                    new AdaptiveTextBlock
            {
                Text = CardsConfigText.DialogExecuteText,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            },
            new AdaptiveTextBlock
            {
                Text = prompt,
                Wrap = true,
                Size = AdaptiveTextSize.Medium,
                Weight = AdaptiveTextWeight.Default
            },
             new AdaptiveTextBlock
            {
                Text = user,
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Lighter
            }
        }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static Attachment CreatePromptFormCard(string promptId, string prompt)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = CardsConfigText.DialogHeaderText,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Add the complete prompt text
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = prompt,
                Wrap = true,
                Size = AdaptiveTextSize.Medium,
                Weight = AdaptiveTextWeight.Default
            });

            // Regular expression to detect placeholders:
            // - Words in curly brackets separated by pipes for dropdowns
            // - Words enclosed in single or double curly brackets for text inputs
            var regexPattern = @"{{?(\w+)}?}|\{(\w+)\|([\w\|]+)\}";
            var matches = Regex.Matches(prompt, regexPattern);

            // Use a HashSet to store unique placeholders
            HashSet<string> addedPlaceholders = new HashSet<string>();

            foreach (Match match in matches.Cast<Match>())
            {
                // Check if the placeholder is for a dropdown (curly brackets with pipes)
                if (match.Groups[2].Success && match.Groups[3].Success)
                {
                    string dropdownLabel = match.Groups[2].Value;
                    string[] options = match.Groups[3].Value.Split('|');

                    // Skip if already added
                    if (addedPlaceholders.Contains(dropdownLabel))
                        continue;

                    addedPlaceholders.Add(dropdownLabel);

                    card.Body.Add(new AdaptiveChoiceSetInput
                    {
                        Id = dropdownLabel,
                        Placeholder = "Selecteer een " +  dropdownLabel,
                        Choices = options.Select(o => new AdaptiveChoice { Title = o, Value = o }).ToList(),
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsRequired = true
                    });
                }
                else
                {
                    // Create an input field for each unique matching word
                    string inputLabel = match.Groups[1].Value;

                    // Skip the placeholder if it has already been added
                    if (addedPlaceholders.Contains(inputLabel))
                        continue;

                    addedPlaceholders.Add(inputLabel); // Remember the placeholder we added

                    bool isMultiLine = match.Value.StartsWith("{{") && match.Value.EndsWith("}}");

                    card.Body.Add(new AdaptiveTextInput
                    {
                        Id = inputLabel,
                        Placeholder = inputLabel,
                        IsRequired = true,
                        IsMultiline = isMultiLine // Set to true if surrounded by double curly brackets
                    });
                }
            }

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Id = "promptForm",
                Data = new { ActionType = CardsConfigCommands.PromptFormAction, PromptId = promptId, SourcePrompt = prompt },
                Title = CardsConfigText.OkText
            });

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }


        public static Attachment CreateEditPromptCard(Prompt selectedPrompt, IEnumerable<Assistant> assistants,
        IEnumerable<Function> functions, IEnumerable<string> categories)
        {
            var assistantChoices = assistants.Select(a => new AdaptiveChoice { Title = a.Name, Value = a.Id.ToString() }).ToList();
            var categoryChoices = categories.Order().Select(f => new AdaptiveChoice { Title = f, Value = f }).ToList();
            var functionChoices = functions.OrderBy(a => a.Title).Select(f => new AdaptiveChoice { Title = f.Title, Value = f.Id.ToString() }).ToList();
            var visibilityChoices = Enum.GetValues(typeof(Visibility)).Cast<Visibility>().Select(a => new AdaptiveChoice { Title = a.ToText(), Value = a.ToText() }).ToList();
            var selectedFunctionValues = selectedPrompt.Functions.Select(f => f.Id.ToString()).ToList();

            // Adding this line to create a text representation of the functions
            var currentFunctionsText = string.Join(", ", selectedPrompt.Functions.Select(f => f.Title));
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
            // Title Row
                    new AdaptiveTextBlock
                    {
                        Text = CardsConfigText.EditDialogText,
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large
                    },
                    // Edit Title Field
                    new AdaptiveTextInput
                    {
                        Id = "PromptTitle",
                        IsRequired = true,
                        Label = CardsConfigText.AiDialogNameText,
                        Placeholder = CardsConfigText.AiDialogNameText,
                        Value = selectedPrompt.Title
                    },
                    // Edit Content Field
                    new AdaptiveTextInput
                    {
                        Id = "PromptContent",
                        Label = "Dialoog",
                            IsRequired = true,
                        Placeholder = CardsConfigText.AiDialogContentText,
                        Value = selectedPrompt.Content,
                        IsMultiline = true
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "Category",
                        Choices = categoryChoices,
                        Placeholder = CardsConfigText.SelectCategoryText,
                        Label = CardsConfigText.Category,
                        Style = AdaptiveChoiceInputStyle.Compact,
                        Value = selectedPrompt.Category
                    },
                         new AdaptiveChoiceSetInput
                    {
                        Id = "VisibilityChoice",
                        Choices = visibilityChoices,
                        IsRequired = true,
                        Placeholder = CardsConfigText.SelectVisibilityText,
                        Label = CardsConfigText.Visibility,
                        Style = AdaptiveChoiceInputStyle.Compact,
                        Value = selectedPrompt.Visibility.ToText()
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "AssistantChoice",
                        Choices = assistantChoices,
                        Placeholder = CardsConfigText.SelectAiAssistantText,
                        Label = CardsConfigText.AiAssistantText,
                        Style = AdaptiveChoiceInputStyle.Compact,
                        Value = selectedPrompt.Assistant?.Id
                    },

                      new AdaptiveChoiceSetInput
            {
                Id = "FunctionChoices",
                Choices = functionChoices,
                Label = "Functies",
                IsMultiSelect = true,
                Placeholder = CardsConfigText.SelectFunctions,
                Wrap = true,
                Value = string.Join(",", selectedFunctionValues)
            },
                  },
                Actions = {
                    new AdaptiveSubmitAction
                    {
                        Title = CardsConfigText.OkText,
                        Data = new { ActionType = CardsConfigCommands.SavePromptAction, ResourceId = selectedPrompt.Id }
                    },

                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }


        public static Attachment CreatePromptEditedCard(Prompt selectedPrompt, string currentUser)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                       new AdaptiveTextBlock
            {
                Text = CardsConfigText.DialogEditedText,
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Large
            },
            // Title Row
            new AdaptiveTextBlock
            {
                Text = selectedPrompt.Title,
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium
            },
            // Content Field
            new AdaptiveTextBlock
            {
                Id = "PromptContent",
                Wrap = true,
                Text = selectedPrompt.Content,
                Size = AdaptiveTextSize.Small
            },
            new AdaptiveFactSet
            {
                Spacing = AdaptiveSpacing.Padding,
                Facts = new List<AdaptiveFact>
                {
                    new AdaptiveFact { Title = "Eigenaar", Value = selectedPrompt.Owner?.DisplayName },
                    new AdaptiveFact { Title = "Zichtbaarheid", Value = selectedPrompt.Visibility == Visibility.Department ? selectedPrompt.Department?.Name : selectedPrompt.Visibility == Visibility.Owner ? selectedPrompt.Owner.DisplayName : CardsConfigText.EveryoneText },
                    new AdaptiveFact { Title = "Categorie", Value = selectedPrompt.Category },
                    new AdaptiveFact { Title = "AI-assistent", Value = selectedPrompt.Assistant?.Name },
                    new AdaptiveFact { Title = "Functies", Value = string.Join(", ", selectedPrompt.Functions?.Select(a => a.Title)) }
                }
            }
        },
                Actions = {
            new AdaptiveSubmitAction
            {
                Title = CardsConfigText.ExecuteText,
                Data = new { ActionType = CardsConfigCommands.ExecutePromptAction, ExecutePromptId = selectedPrompt.Id }
            }
        }
            };

            if (selectedPrompt.Owner.DisplayName == currentUser)
            {
                card.Actions.Add(new AdaptiveSubmitAction
                {
                    Title = "Wijzigen",
                    Data = new { ActionType = CardsConfigCommands.EditPromptAction, ResourceId = selectedPrompt.Id }
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

