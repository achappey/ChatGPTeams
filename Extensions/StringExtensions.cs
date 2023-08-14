using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace achappey.ChatGPTeams.Extensions
{
    public static class StringExtensions
    {
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
                    // URL-decode and parse as JSON
                    //  var jsonStr = HttpUtility.UrlDecode(parameterValue);
                    var jsonObj = JObject.Parse(parameterValue);
                    // Extract objectUrl from the JSON object
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



    }
}