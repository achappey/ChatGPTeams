using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {

        [MethodDescription("Timeline|Search for timeline messages using multiple filters.")]
        public async Task<string> SearchTimelineMessages(
            [ParameterDescription("Created at or after this date and time in ISO 8601 format (yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
            [ParameterDescription("Created at or before this date and time in ISO 8601 format (yyyy-MM-dd HH:mm:ss).")] string createdBefore = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();
            createdBefore?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;
            if (!string.IsNullOrEmpty(createdBefore)) filters["[created_at][le]"] = createdBefore;

            return await FetchSimplicateHtmlData<TimelineMessage>(filters, "timeline/message", pageNumber);
        }

    }
}
