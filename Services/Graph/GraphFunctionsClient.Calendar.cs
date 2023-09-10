using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {
        [MethodDescription("Calendar|Gets events for the specified user.")]
        public async Task<string> SearchEvents(
                [ParameterDescription("User id of the calendar")] string userId,
                [ParameterDescription("Subject of the event to search for")] string subject = null,
                [ParameterDescription("Organizer of the event to search for")] string organizer = null,
                [ParameterDescription("Date in ISO 8601 format")] string date = null,
                [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterQueries = new List<string>();

            if (!string.IsNullOrEmpty(subject))
            {
                filterQueries.Add($"contains(subject, '{subject}')");
            }

            if (!string.IsNullOrEmpty(organizer))
            {
                filterQueries.Add($"organizer/emailAddress/address eq '{organizer}'");
            }

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedFromDate))
            {
                DateTime parsedToDate = parsedFromDate.AddDays(1);
                filterQueries.Add($"start/dateTime ge '{parsedFromDate:s}Z'");
                filterQueries.Add($"end/dateTime lt '{parsedToDate:s}Z'");
            }

            var filterOptions = new List<QueryOption>();
            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var filterQuery = string.Join(" and ", filterQueries);
            var selectQuery = "id,subject,start,end";

            var events = await graphClient.Users[userId].Events
                .Request(filterOptions)
                .Filter(filterQuery)
                .Select(selectQuery)
                .GetAsync();

            return events.CurrentPage.Select(_mapper.Map<Models.Graph.Event>).ToHtmlTable(events.NextPageRequest?.QueryOptions.FirstOrDefault(a => a.Name == "$skiptoken")?.Value);
        }



    }
}