using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Models;
using HtmlAgilityPack;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Extensions
{
    public static class GraphExtensions
    {


        public static string GetFieldValue(this ListItem message, string field)
        {
            if (message.Fields.AdditionalData.ContainsKey(field))
            {
                return message.Fields.AdditionalData[field]?.ToString();
            }

            return null;

        }


        public static IEnumerable<T> GetFieldValues<T>(this ListItem message, string fieldName, Func<JsonElement, T> converter)
        {
            if (message.Fields.AdditionalData.TryGetValue(fieldName, out object value) &&
                value is JsonElement element &&
                element.ValueKind == JsonValueKind.Array)
            {
                var result = new List<T>();

                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        result.Add(converter(item));
                    }
                }

                return result;
            }

            return new List<T>();
        }

        private static Models.User ConvertToUser(JsonElement item)
        {
            return new Models.User()
            {
                Id = item.GetProperty("LookupId").GetInt32(),
                DisplayName = item.GetProperty("LookupValue").ToString()
            };
        }

        private static LookupField ConvertToLookupField(JsonElement item)
        {
            return new LookupField()
            {
                LookupId = item.GetProperty("LookupId").GetInt32(),
                LookupValue = item.GetProperty("LookupValue").ToString()
            };
        }

        public static IEnumerable<Models.User> GetOwners(this ListItem message)
        {
            return GetFieldValues(message, FieldNames.AIOwners, ConvertToUser);
        }

        public static IEnumerable<Models.User> GetReaders(this ListItem message)
        {
            return GetFieldValues(message, FieldNames.AIReaders, ConvertToUser);
        }

        public static IEnumerable<LookupField> GetFieldValues(this ListItem message, string field)
        {
            return GetFieldValues(message, field, ConvertToLookupField);
        }
        public static string ToUrl(this Microsoft.Bot.Schema.Attachment attachment)
        {
            switch (attachment.ToContentType())
            {
                case Models.ContentType.HTML:
                    var href = attachment.Content?.ToString().ExtractHref();

                    if (href.StartsWith("http"))
                    {
                        return href;
                    }

                    return null;
                case Models.ContentType.DOWNLOAD:
                    return attachment.ContentUrl;
                default:
                    return null;
            }
        }

        public static IEnumerable<Resource> ExtractResources(this Microsoft.Bot.Schema.Attachment attachment)
        {
            var resources = new List<Resource>();

            switch (attachment.ToContentType())
            {
                case Models.ContentType.HTML:
                    var htmlContent = attachment.Content?.ToString();
                    var hrefs = ExtractAllHrefs(htmlContent);

                    foreach (var href in hrefs)
                    {
                        if (href.StartsWith("http"))
                        {
                            resources.Add(new Resource { Url = href, Name = attachment.ToAttachmentName(), Id = "" });
                        }
                    }

                    break;
                case Models.ContentType.DOWNLOAD:
                    resources.Add(new Resource { Url = attachment.ContentUrl, Name = attachment.Name, Id = "" });
                    break;
                default:
                    break;
            }

            return resources;
        }

        private static IEnumerable<string> ExtractAllHrefs(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent)) return Enumerable.Empty<string>();

            var hrefs = new List<string>();
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            var anchorTags = document.DocumentNode.Descendants("a");

            foreach (var anchorTag in anchorTags)
            {
                var hrefValue = anchorTag.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(hrefValue))
                {
                    hrefs.Add(hrefValue);
                }
            }

            return hrefs;
        }


        public static string ToUrl(this ChatMessageAttachment attachment)
        {
            switch (attachment.ToContentType())
            {
                case Models.ContentType.HTML:
                    var href = attachment.Content?.ToString().ExtractHref();

                    if (href.StartsWith("http"))
                    {
                        return href;
                    }

                    return null;
                case Models.ContentType.DOWNLOAD:
                    return attachment.ContentUrl;
                default:
                    return null;
            }
        }

        public static string ToLookupField(this string value) => value + "LookupId";

        public static Models.ContentType? ToContentType(this Microsoft.Bot.Schema.Attachment attachment) => attachment.ContentType switch
        {
            "text/html" => (Models.ContentType?)Models.ContentType.HTML,
            "application/vnd.microsoft.teams.file.download.info" => (Models.ContentType?)Models.ContentType.DOWNLOAD,
            _ => null,
        };

        public static Models.ContentType? ToContentType(this ChatMessageAttachment attachment) => attachment.ContentType switch
        {
            "text/html" => (Models.ContentType?)Models.ContentType.HTML,
            "application/vnd.microsoft.teams.file.download.info" => (Models.ContentType?)Models.ContentType.DOWNLOAD,
            _ => null,
        };

        public static ListItem ToListItem(this Dictionary<string, object> dict) => new()
        {
            Fields = dict.ToFieldValueSet()
        };

        public static FieldValueSet ToFieldValueSet(this Dictionary<string, object> dict) => new()
        {
            AdditionalData = dict
        };

    }
}