using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {
        [MethodDescription("Gets events for the user using the Microsoft Graph API")]
        public async Task<IEnumerable<Models.Graph.Event>> SearchEvents(
            [ParameterDescription("User e-mail of the calendar")] string userMail = null,
            [ParameterDescription("Subject of the event to search for")] string subject = null,
            [ParameterDescription("Organizer of the event to search for")] string organizer = null,
            [ParameterDescription("Start date in ISO 8601 format.")] string fromDate = null,
            [ParameterDescription("End date in ISO 8601 format")] string toDate = null)
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

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out DateTime parsedFromDate))
            {
                filterQueries.Add($"start/dateTime ge '{parsedFromDate:s}Z'");
            }

            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out DateTime parsedToDate))
            {
                filterQueries.Add($"end/dateTime le '{parsedToDate:s}Z'");
            }

            var filterQuery = string.Join(" and ", filterQueries);
            var selectQuery = "id,webLink,bodyPreview,subject,start,end";

            var client = string.IsNullOrEmpty(userMail) ? graphClient.Me : graphClient.Users[userMail];

            var events = await client.Events
                .Request()
                .Filter(filterQuery)
                .Select(selectQuery)
                .GetAsync();

            return events
                .Take(10)
                .Select(a => _mapper.Map<Models.Graph.Event>(a));
        }



    }
}