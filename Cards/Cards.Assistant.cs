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




        public static Attachment CreateRoleInfoCard(string title,
                                                    string role,
                                                    float temperature,
                                                    string model,
                                                    Visibility visibility,
                                                    IEnumerable<Function> assistantFunctions,
                                                    IEnumerable<string> owners,
                                                    IEnumerable<string> roles,
                                                    IEnumerable<Function> functions,
                                                    bool isOwner)
        {
            List<AdaptiveChoice> roleChoices = roles.Select(r => new AdaptiveChoice { Title = r, Value = r }).ToList();
            var functionChoices = functions.OrderBy(a => a.Title).Select(f => new AdaptiveChoice { Title = f.Title, Value = f.Id.ToString() }).ToList();
            var visibilityChoices = Enum.GetValues(typeof(Visibility)).Cast<Visibility>().Select(a => new AdaptiveChoice { Title = a.ToText(), Value = a.ToText() }).ToList();
            var selectedFunctionValues = assistantFunctions.Select(f => f.Id.ToString()).ToList();
            List<AdaptiveChoice> temperatureChoices = Enumerable.Range(1, 10).Select(t =>
                new AdaptiveChoice { Title = Math.Round(t * 0.1, 1).ToString("F1"), Value = Math.Round(t * 0.1, 1).ToString("F1") }).Reverse().ToList();

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = {
                new AdaptiveContainer
                {
                    Items = {
                        new AdaptiveTextBlock
                                {
                                    Text = CardsConfigText.SelectYourAiAssistantText,
                                    Size = AdaptiveTextSize.Large,
                                    Weight = AdaptiveTextWeight.Bolder
                                },
                                new AdaptiveTextBlock
                        {
                            Text = title,
                            Weight = AdaptiveTextWeight.Bolder,
                            Size = AdaptiveTextSize.Medium
                        }
                    }
                },
                new AdaptiveContainer
                {
                    Items = {
                        new AdaptiveTextBlock
                        {
                            Text = role,
                             Size = AdaptiveTextSize.Small,
                            Wrap = true
                        },
                        new AdaptiveFactSet
                        {
                            Spacing = AdaptiveSpacing.ExtraLarge,
                            Facts = {
                                new AdaptiveFact(CardsConfigText.CreativityLevelText, temperature.ToString()),
                                new AdaptiveFact("Model", model),
                                new AdaptiveFact { Title = "Zichtbaarheid", Value = visibility.ToText() },
                                new AdaptiveFact("Eigenaren", string.Join(", ", owners)),
                                new AdaptiveFact { Title = "Functies", Value = string.Join(", ", assistantFunctions?.Select(a => a.Title)) }
                            }
                        }
                    }
                }
            },
                Actions = {
            new AdaptiveShowCardAction
            {
                Title = CardsConfigText.ChangeAiAssistantText,
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = {
                        new AdaptiveTextBlock
                        {
                            Text = CardsConfigText.AiAssistantText,
                            Wrap = true
                        },
                        new AdaptiveChoiceSetInput()
                        {
                            Id = "Role",
                            Choices = roleChoices,
                            Placeholder = CardsConfigText.SelectAiAssistantText,
                            IsRequired = true,
                            Style = AdaptiveChoiceInputStyle.Compact
                        }
                    },
                    Actions = {
                        new AdaptiveSubmitAction
                        {
                            Title = CardsConfigText.OkText,
                            Data = new { ActionType = CardsConfigCommands.UpdateRoleAction }
                        }
                    }
                }
            },
            new AdaptiveShowCardAction
            {
                Title = CardsConfigText.EditAssistantText,
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = {
                        new AdaptiveTextBlock
                        {
                            Text = CardsConfigText.EditAssistantText,
                            Wrap = true
                        },
                        new AdaptiveTextInput()
                        {
                            Id = CardsConfigCommands.NewName,
                            Value = title,
                            IsVisible = isOwner,
                            Placeholder = CardsConfigText.NameText,
                            Label = CardsConfigText.NameText,
                            IsRequired = true,
                        },
                        new AdaptiveTextInput()
                        {
                            Id = CardsConfigCommands.NewRole,
                            Value = role,
                            Label =  CardsConfigText.EnterRoleText,
                            IsMultiline = true,
                            IsVisible = isOwner,
                            Placeholder = CardsConfigText.EnterRoleText,
                            IsRequired = true,
                        },
                        new AdaptiveChoiceSetInput()
                        {
                            Id = CardsConfigCommands.NewTemperature,
                            Choices = temperatureChoices,
                            Placeholder = CardsConfigText.CreativityLevelExtendedText,
                            IsRequired = true,
                            Label =  CardsConfigText.CreativityLevelExtendedText,
                            Style = AdaptiveChoiceInputStyle.Compact,
                            Value = temperature.ToString("F1")
                        },
                         new AdaptiveChoiceSetInput()
                        {
                            Id = "VisibilityChoice",
                            Choices = visibilityChoices,
                            Placeholder = CardsConfigText.SelectVisibilityText,
                            Label = CardsConfigText.Visibility, // Update this text as needed
                            IsRequired = true,
                            Style = AdaptiveChoiceInputStyle.Compact,
                            IsVisible = isOwner,
                             Value = visibility.ToText()
                        },
                            new AdaptiveChoiceSetInput()
                        {
                            Id = "FunctionChoices",
                            Choices = functionChoices, // Assuming functionChoices is a list of AdaptiveChoice for the functions
                            Label = "Functies",
                            Placeholder = CardsConfigText.SelectFunctions,
                            IsMultiSelect = true,
                            Value = string.Join(",", selectedFunctionValues),
                            IsVisible = isOwner
                        }
                    },
                    Actions = {
                        new AdaptiveSubmitAction
                        {
                            Title = CardsConfigText.OkText,
                            Data = new { ActionType = CardsConfigCommands.EditAssistantAction },
                            IsEnabled = isOwner
                        }
                    }
                }
            },
            new AdaptiveSubmitAction
                        {
                            Title = CardsConfigText.CloneText,
                            Data = new { ActionType = CardsConfigCommands.CloneAssistantAction }
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

