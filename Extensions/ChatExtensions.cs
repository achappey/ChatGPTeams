using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace achappey.ChatGPTeams.Extensions
{
    public static class ChatExtensions
    {


        public static (string ChannelId, string MessageId) ExtractIds(this string source)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));

            var parts = source.Split(';');
            if (parts.Length != 2) throw new FormatException("Invalid source format. Expected format: channelId;messageId.");

            var channelId = parts[0];
            var messageIdPair = parts[1].Split('=');
            if (messageIdPair.Length != 2 || messageIdPair[0] != "messageid") throw new FormatException("Invalid messageId format. Expected format: messageId=value.");

            var messageId = messageIdPair[1];

            return (channelId, messageId);
        }


        public static string ToAttachmentName(this Microsoft.Bot.Schema.Attachment url)
        {
            if (!string.IsNullOrEmpty(url.Name))
            {
                return url.Name;
            }

            var urlValue = url.ToUrl();

            if (urlValue != null)
            {
                var uri = new Uri(urlValue);
                string baseUrl = uri.GetLeftPart(UriPartial.Path);
                return baseUrl;
            }

            return null;

        }


        public static string ConvertHtmlToPlainText(this string html)
        {
            var tagRegex = new Regex("<.*?>", RegexOptions.Compiled);

            var text = tagRegex.Replace(html, string.Empty);

            return text;
        }

        public static bool IsOutlookUrl(this string url)
        {
            // Regex pattern to match the specified URL formats, allowing for additional paths or parameters
            string pattern = @"^https:\/\/outlook\.office365\.com.*$";
            var regex = new Regex(pattern);

            // Check if the URL matches the pattern
            if (regex.IsMatch(url))
                return true;

            return false;
        }

        public static bool IsSharePointUrl(this string url)
        {
            // Regex pattern to match the specified URL formats, allowing for additional paths or parameters
            string pattern = @"^https:\/\/(.+?)(-my)?\.sharepoint\.com.*$";
            var regex = new Regex(pattern);

            // Check if the URL matches the pattern
            if (regex.IsMatch(url))
                return true;

            return false;
        }


        public static List<string> GetLinesAsync(this string filename, byte[] data)
        {
            string extension = Path.GetExtension(filename).ToLowerInvariant();

            switch (extension)
            {
                case ".pdf":
                    return data.ConvertPdfToLines();
                case ".docx":
                    return data.ConvertDocxToLines();
                case ".csv":
                    return data.ConvertCsvToList();
                case ".pptx":
                    return data.ConvertPptxToLines();
                case ".html":
                    return data.ConvertHtmlToList();
                case ".txt":
                    return data.ConvertTxtToList();
                default:
                    break;

            }

            return null;
        }

        public static Role ToRole(this object role)
        {
            switch (role.ToString())
            {
                case "user":
                    return Role.user;
                case "assistant":
                    return Role.assistant;
                case "function":
                    return Role.function;
                case "system":
                    return Role.system;
                default:
                    throw new ArgumentException("Role missing");
            }
        }

        public static async Task<ConversationContext> ToConversationContext(this ITurnContext turnContext)
        {
            var conversationContext = new ConversationContext()
            {
                Id = turnContext.Activity.Conversation.Id,
                LocalTimezone = turnContext.Activity.LocalTimezone,
                UserDisplayName = turnContext.Activity.From.Name,
                ChatType = turnContext.Activity.Conversation.ConversationType.ToChatType(),
                ReplyToId = turnContext.Activity.ReplyToId,
                MessageId = turnContext.Activity.Id
            };

            if (turnContext.Activity.Conversation.ConversationType == "channel")
            {
                var (ChannelId, MessageId) = turnContext.Activity.Conversation.Id.ExtractIds();
                var teamsInfo = await TeamsInfo.GetTeamDetailsAsync(turnContext);

                conversationContext.TeamsId = teamsInfo.AadGroupId;
                conversationContext.ChannelId = ChannelId;
                conversationContext.MessageId = MessageId;
            }

            return conversationContext;
        }


        public static ChatType ToChatType(this string type)
        {
            switch (type)
            {
                case "personal":
                    return ChatType.personal;
                case "groupChat":
                    return ChatType.groupchat;
                case "channel":
                    return ChatType.channel;
            }

            throw new Exception("Unknown chat type");
        }

        public static Visibility ToVisibility(this string type)
        {
            switch (type)
            {
                case "Owner":
                    return Visibility.Owner;
                case "Department":
                    return Visibility.Department;
                case "Everyone":
                    return Visibility.Everyone;
            }

            throw new Exception("Unknown visibility");
        }

        public static IEnumerable<Function> ToFunctions(this string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }
            var items = type.Split(",");

            return items.Select(a => new Function()
            {
                Id = a
            });
        }

        public static Visibility TextToVisibility(this string type)
        {
            switch (type)
            {
                case CardsConfigText.Owner:
                    return Visibility.Owner;
                case CardsConfigText.DepartmentText:
                    return Visibility.Department;
                case CardsConfigText.EveryoneText:
                    return Visibility.Everyone;
            }

            throw new Exception("Unknown visibility");
        }

        public static string ToText(this Visibility type)
        {
            switch (type)
            {
                case Visibility.Owner:
                    return CardsConfigText.Owner;
                case Visibility.Department:
                    return CardsConfigText.DepartmentText;
                case Visibility.Everyone:
                    return CardsConfigText.EveryoneText;
            }

            throw new Exception("Unknown visibility");
        }

        public static string ExtractHref(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            var match = Regex.Match(str, @"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>\S+))", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }


        public static string GetLastResult(this ITurnContext turnContext)
        {
            string resultValue = null;

            // If the TurnState contains "turn" key
            if (turnContext.TurnState.ContainsKey("turn"))
            {
                // Extract the object with key "turn"
                var turnObject = turnContext.TurnState["turn"];
                // Check if the object is a JObject
                if (turnObject is JObject jObject)
                {
                    // Try to extract "lastresult"
                    if (jObject.TryGetValue("lastresult", out JToken lastResult))
                    {
                        // Convert lastResult into a string and assign to resultValue
                        resultValue = lastResult.ToString();
                    }
                }
            }

            return resultValue;
        }


        public static OpenAI.ObjectModels.RequestModels.ChatMessage ToChatMessage(this Assistant assistant)
        {
            return new OpenAI.ObjectModels.RequestModels.ChatMessage("system", assistant.Prompt);
        }



    }
}