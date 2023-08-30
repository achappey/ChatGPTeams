using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        public static Attachment CreateExecutePromptCard(string prompt, string title, string user)
        {
            // parse the JSON string to a dictionary

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                    new AdaptiveTextBlock
            {
                Text = CardsConfigText.DialogExecuteText + ": " +title,
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

        public static Attachment CreatePromptFormCard(string promptId, string title, string prompt)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = CardsConfigText.DialogHeaderText + ": " + title,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });


            var regexPattern = @"\{(\w+)(?:\(type=(\w+),?(?:multi=(true|false))?,?(?:required=(true|false))?,?(?:options=([^)]+))?,?(?:min=(\d+))?,?(?:max=(\d+))?\))?\}";

            var matches = Regex.Matches(prompt, regexPattern);

            StringBuilder simplifiedPrompt = new StringBuilder();
            int lastIndex = 0;

            foreach (Match match in matches.Cast<Match>())
            {
                // Voeg de tekst tussen de laatste match en deze match toe.
                simplifiedPrompt.Append(prompt.Substring(lastIndex, match.Index - lastIndex));

                // Voeg de vereenvoudigde placeholder toe.
                string fieldName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[7].Value;
                simplifiedPrompt.Append("{" + fieldName + "}");

                lastIndex = match.Index + match.Length;
            }

            // Voeg de rest van de originele prompt toe.
            simplifiedPrompt.Append(prompt.Substring(lastIndex));

            // Voeg nu simplifiedPrompt toe aan de AdaptiveTextBlock
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = simplifiedPrompt.ToString(),
                Wrap = true,
                Size = AdaptiveTextSize.Medium,
                Weight = AdaptiveTextWeight.Default
            });

            HashSet<string> addedPlaceholders = new HashSet<string>();

            foreach (Match match in matches.Cast<Match>())
            {
                string fieldName = match.Groups[1].Value;
                string type = match.Groups[2].Value;
                string multi = match.Groups[3]?.Value;
                string required = match.Groups[4]?.Value;
                string options = match.Groups[5]?.Value;

                bool isMultiselect = multi == "true";
                bool isRequired = required == "true";

                string min = match.Groups[6]?.Value;
                string max = match.Groups[7]?.Value;

                if (!addedPlaceholders.Contains(fieldName))
                {
                    addedPlaceholders.Add(fieldName);

                    switch (type)
                    {
                        case "text":
                            card.Body.Add(new AdaptiveTextInput
                            {
                                Id = fieldName,
                                Label = fieldName,
                                IsRequired = isRequired,
                                IsMultiline = isMultiselect
                            });
                            break;

                        case "select":
                            var choiceSet = new AdaptiveChoiceSetInput
                            {
                                Id = fieldName,
                                Label = fieldName,
                                Placeholder = "Selecteer een " + fieldName,
                                IsRequired = isRequired,
                                IsMultiSelect = isMultiselect,
                                Choices = options?.Split('|').Select(o => new AdaptiveChoice { Title = o, Value = o }).ToList()
                            };
                            card.Body.Add(choiceSet);
                            break;


                        case "person":
                            var choiceSetInput = new AdaptiveChoiceSetInput
                            {
                                Id = fieldName,
                                Label = fieldName,
                                IsRequired = isRequired,
                                IsMultiSelect = isMultiselect,
                                Choices = new List<AdaptiveChoice>(),
                            };

                            choiceSetInput.AdditionalProperties["choices.data"] = new
                            {
                                type = "Data.Query",
                                dataset = "graph.microsoft.com/users"
                            };

                            card.Body.Add(choiceSetInput);
                            break;

                        case "date":
                            card.Body.Add(new AdaptiveDateInput()
                            {
                                Id = fieldName,
                                Label = fieldName,
                                IsRequired = isRequired,
                                Placeholder = "Selecteer een " + fieldName
                            });
                            break;

                        case "number":
                            double? parsedMin = string.IsNullOrEmpty(min) ? null : Double.Parse(min);
                            double? parsedMax = string.IsNullOrEmpty(max) ? null : Double.Parse(max);

                            var numberInput = new AdaptiveNumberInput()
                            {
                                Id = fieldName,
                                Label = fieldName,
                                IsRequired = isRequired,
                                Placeholder = "Voer een " + fieldName + " in"
                            };

                            if (parsedMin.HasValue)
                            {
                                numberInput.Min = parsedMin.Value;
                            }

                            if (parsedMax.HasValue)
                            {
                                numberInput.Max = parsedMax.Value;
                            }

                            card.Body.Add(numberInput);
                            break;

                        case "time":
                            card.Body.Add(new AdaptiveTimeInput()
                            {
                                Id = fieldName,
                                Label = fieldName,
                                IsRequired = isRequired,
                                Placeholder = "Selecteer een " + fieldName
                            });
                            break;
                    }
                }
            }

            card.Actions.Add(new AdaptiveSubmitAction
            {
                Id = "promptForm",
                Data = new
                {
                    ActionType = CardsConfigCommands.PromptFormAction,
                    PromptId = promptId,
                    SourcePrompt = prompt
                },
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

            var factSet = new AdaptiveFactSet
            {
                Spacing = AdaptiveSpacing.Small,
                
                Facts = new List<AdaptiveFact>
    {
        new AdaptiveFact("Tekst", "{veldNaam(type=text)}"),
        new AdaptiveFact("Meerdere regels", "{veldNaam(type=text,multi=true)}"),
        new AdaptiveFact("Dropdown", "{veldNaam(type=select,options=Optie1|Optie2|...)}"),
        new AdaptiveFact("Persoon", "{veldNaam(type=person)}"),
        new AdaptiveFact("Meerdere personen", "{veldNaam(type=person,multi=true)}"),
        new AdaptiveFact("Datum", "{veldNaam(type=date)}"),
        new AdaptiveFact("Tijd", "{veldNaam(type=time)}"),
        new AdaptiveFact("Nummer", "{veldNaam(type=number)}"),
        new AdaptiveFact("Verplicht", "{veldNaam(required=true)}")
    }
            };

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
                                      new AdaptiveTextBlock
            {
                Id = "Dialoogvelden",
                Text = "Veldtypes",
            },

                                factSet,

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
                     new AdaptiveSubmitAction
                    {
                        Title = CardsConfigText.RemoveText,
                        Data = new { ActionType = CardsConfigCommands.DeletePromptAction, ResourceId = selectedPrompt.Id }
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

