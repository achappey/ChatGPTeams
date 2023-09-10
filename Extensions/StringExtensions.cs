using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
using achappey.ChatGPTeams.Services.Simplicate;
using System.Collections;
using System.Runtime.Serialization;

namespace achappey.ChatGPTeams.Extensions
{
    public static class StringExtensions
    {



        public static string GetEnumMemberAttributeValue<T>(T enumValue) where T : Enum
        {
            var type = typeof(T);
            var memberInfos = type.GetMember(enumValue.ToString());
            var enumMemberAttribute = memberInfos[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault() as EnumMemberAttribute;

            return enumMemberAttribute?.Value;
        }

        public static int PageSize = 10;

        public static int CalculateTotalPages(this Metadata metadata)
        {
            if (metadata == null || PageSize <= 0) return 1;
            return (int)Math.Ceiling((double)metadata.Count / PageSize);
        }

        public static string ToHtmlTableText<T>(this IEnumerable<T> items, string captionText)
        {
            string html = "<table>";

            if (!string.IsNullOrEmpty(captionText))
            {
                html += $"<caption>{captionText}</caption>";
            }

            html += "<thead><tr>";
            AddHeaders(typeof(T), ref html, "");
            html += "</tr></thead>";

            html += "<tbody>";
            foreach (T item in items)
            {
                html += "<tr>";
                AddValues(item, ref html, "");
                html += "</tr>";
            }
            html += "</tbody></table>";

            return html;
        }

        public static string ToHtmlTable<T>(this IEnumerable<T> items, string skipToken)
        {
            string captionText = string.IsNullOrEmpty(skipToken)
                ? string.Empty
                : $"More items are available. Use the skipToken parameter with the value {skipToken} in your next function call to retrieve the next page of results.";

            return items.ToHtmlTableText(captionText);
        }

        public static string ToHtmlTable<T>(this IEnumerable<T> items, long currentPage, int totalPages, int totalItems)
        {
            string captionText = $"Current page: {currentPage}/{totalPages} (you are viewing page {currentPage} out of {totalPages} available pages) <br> Items per page: {PageSize} (each page displays up to {PageSize} items) <br> Total items: {totalItems} (there are {totalItems} items in the dataset)";

            return items.ToHtmlTableText(captionText);
        }



        private static void AddHeaders(Type type, ref string html, string prefix)
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.DeclaringType?.Assembly == type.Assembly)
                {
                    if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
                    {
                        html += $"<th>{prefix}{property.Name}</th>";
                    }
                    else if (property.PropertyType.IsArray)
                    {
                        html += $"<th>{prefix}{property.Name}[]</th>";
                    }
                    else
                    {
                        AddHeaders(property.PropertyType, ref html, $"{prefix}{property.Name}:");
                    }
                }
            }
        }

        private static void AddHeaders2(Type type, ref string html, string prefix)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                {
                    html += $"<th>{prefix}{property.Name}</th>";
                }
                else if (property.PropertyType.IsArray)
                {
                    html += $"<th>{prefix}{property.Name}[]</th>";
                }
                else
                {
                    AddHeaders(property.PropertyType, ref html, $"{prefix}{property.Name}:");
                }
            }
        }


        private static void AddValues(object item, ref string html, string prefix)
        {
            foreach (PropertyInfo property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length == 0) // Voorkomt dat geïndexeerde eigenschappen worden geëvalueerd
                {
                    object value = property.GetValue(item);

                    if (value == null)
                    {
                        html += "<td></td>";
                    }
                    else if (value.GetType().IsPrimitive || value is string || value is DateTime || value is DateTimeOffset)
                    {
                        html += $"<td>{value}</td>";
                    }
                    else if (value is IEnumerable enumerableValue && !(value is string))
                    {
                        html += "<td><table><thead><tr>";
                        var enumerableType = value.GetType().GetGenericArguments().FirstOrDefault();
                        if (enumerableType != null && enumerableValue.Cast<object>().Any())
                        {
                            AddHeaders(enumerableType, ref html, "");
                        }
                        html += "</tr></thead><tbody>";
                        foreach (var arrItem in enumerableValue)
                        {
                            html += "<tr>";
                            AddValues(arrItem, ref html, "");
                            html += "</tr>";
                        }
                        html += "</tbody></table></td>";
                    }
                    else
                    {
                        AddValues(value, ref html, $"{prefix}{property.Name}:");
                    }
                }
            }
        }

        private static void AddValues2(object item, ref string html, string prefix)
        {
            var dsadsa = item.GetType().GetProperties();
            foreach (PropertyInfo property in item.GetType().GetProperties())
            {
                object value = property.GetValue(item);
                if (value == null)
                {
                    html += "<td></td>";
                }
                else if (value.GetType().IsPrimitive || value is string)
                {
                    html += $"<td>{value}</td>";
                }
                else if (value is IEnumerable enumerableValue && !(value is string))
                {
                    html += "<td><table><thead><tr>";
                    var enumerableType = value.GetType().GetGenericArguments().FirstOrDefault();
                    if (enumerableType != null && enumerableValue.Cast<object>().Any())
                    {
                        AddHeaders(enumerableType, ref html, "");
                    }
                    html += "</tr></thead><tbody>";
                    foreach (var arrItem in enumerableValue)
                    {
                        html += "<tr>";
                        AddValues(arrItem, ref html, "");
                        html += "</tr>";
                    }
                    html += "</tbody></table></td>";
                }
                else
                {
                    AddValues(value, ref html, $"{prefix}{property.Name}:");
                }
            }
        }

        public static string ToChatHandle(this string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Define the regex pattern according to the rules
            string pattern = "^[a-zA-Z0-9_-]{1,64}$";

            // Check if the input already matches the pattern
            if (Regex.IsMatch(input, pattern))
                return input;

            // If not, you can trim or modify the string to make it compliant
            // For this example, I'll simply truncate or pad the string, and replace invalid characters

            // Truncate or pad to ensure the length is within 1 to 64 characters
            input = input.Length > 64 ? input.Substring(0, 64) : input.PadRight(1, '_');

            // Replace any characters that are not in the allowed set with an underscore
            input = Regex.Replace(input, "[^a-zA-Z0-9_-]", "_");

            return input;
        }

        public static (string Hostname, string Path, string PageName) ExtractSharePointValues(this string sharePointUrl)
        {
            // Extracting the hostname, site path, and page name from the given URL
            var uri = new Uri(sharePointUrl);
            string hostname = uri.Host;
            string[] pathSegments = uri.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // Assuming the path segment after "sites" is the required path
            int siteIndex = Array.IndexOf(pathSegments, "sites");
            string path = siteIndex >= 0 && pathSegments.Length > siteIndex + 1 ? pathSegments[siteIndex + 1] : string.Empty;

            // Assuming the page name is the last segment in the URL
            string pageName = pathSegments.Length > 0 ? pathSegments[pathSegments.Length - 1] : string.Empty;

            return (Hostname: hostname, Path: path, PageName: pageName);
        }


        public static (string notebookId, string teamsId) ExtractOneNote(this string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');

            string notebookId = null;
            string teamId = null;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "notebooks" && i + 1 < segments.Length)
                {
                    notebookId = segments[i + 1];
                }
                else if (segments[i] == "teams" && i + 1 < segments.Length)
                {
                    teamId = segments[i + 1].Split(".").First();
                }
            }

            return (notebookId, teamId);
        }

        public static int? ToInt(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return int.Parse(text);
        }

        public static string ExtractUrlParameter(this string url, string parameterName)
        {
            try
            {
                var uri = new Uri(url);
                var queryParameters = HttpUtility.ParseQueryString(uri.Query);
                var parameterValue = queryParameters[parameterName];

                // Check if the parameter value is a JSON object
                if (parameterValue.StartsWith("{"))  // %7B = {
                {
                    var jsonObj = JObject.Parse(parameterValue);
                    var urlValue = jsonObj["objectUrl"]?.ToString();
                    return urlValue;
                }
                else
                {
                    // Directly return the URL-decoded parameter value
                    return HttpUtility.UrlDecode(parameterValue);
                }
            }
            catch (Exception)
            {
                // Handle any exceptions that occur during parsing, if necessary
                return null;
            }
        }

        public static T FromJson<T>(this string url)
        {
            return JsonConvert.DeserializeObject<T>(url);
        }

        public static string ExtractContentSource(this string url)
        {
            var notebookSelfUrl = url.ExtractUrlParameter("notebookSelfUrl");

            if (!string.IsNullOrEmpty(notebookSelfUrl))
            {
                return notebookSelfUrl;
            }

            var objectUrl = url.ExtractUrlParameter("subEntityId");

            if (!string.IsNullOrEmpty(objectUrl))
            {
                return objectUrl;
            }

            var aspxUrl = url.ExtractUrlParameter("dest");

            if (!string.IsNullOrEmpty(aspxUrl))
            {
                return aspxUrl;
            }


            return url;

        }


        public static string GenerateNewAssistantTitle(this string title)
        {
            Random random = new Random();
            int number = random.Next(0, 100000);  // upper bound is exclusive so we need to use 100000 to include 99999
            return title + number.ToString("D5"); // "D5" formats the number as a decimal with 5 digits, padding with leading zeroes if necessary
        }

        public static void EnsureValidDateFormat(this string dateStr, string expectedFormat = "yyyy-MM-dd HH:mm:ss")
        {
            DateTime temp;
            if (!string.IsNullOrEmpty(dateStr) && !DateTime.TryParseExact(dateStr, expectedFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out temp))
            {

                throw new Exception("Date format is not OK. Expected format: " + expectedFormat);
            }
        }

    }
}