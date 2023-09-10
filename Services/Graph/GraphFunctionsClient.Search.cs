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


        [MethodDescription("SharePoint|Searches content across SharePoint and OneDrive resources.")]
        public async Task<string> SearchDriveContent(
            [ParameterDescription("The search query.")] string query,
            [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            return await SearchContent(query, EntityType.DriveItem, skipToken);
        }

        [MethodDescription("Mail|Searches Outlook messages.")]
        public async Task<string> SearchOutlookContent(
                    [ParameterDescription("The search query.")] string query,
                    [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            return await SearchContent(query, EntityType.Message, skipToken);
        }

        [MethodDescription("Teams|Searches chat messages.")]
        public async Task<string> SearchChatContent(
                              [ParameterDescription("The search query.")] string query,
                              [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            return await SearchContent(query, EntityType.ChatMessage, skipToken);
        }

        private async Task<string> SearchContent(
                              string query, EntityType type, string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (string.IsNullOrEmpty(query))
            {
                query = "*";
            }

            var filterOptions = new List<QueryOption>();
            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var searchRequest = new SearchRequestObject()
            {
                Query = new SearchQuery
                {
                    QueryString = query,
                },
                EntityTypes = new List<EntityType> { type },
                From = string.IsNullOrEmpty(skipToken) ? 0 : int.Parse(skipToken),
                Size = StringExtensions.PageSize
            };

            var searchResponse = await graphClient.Search
                .Query(new List<SearchRequestObject>() {
                         searchRequest
                })
                .Request(filterOptions)
                .PostAsync();

            return searchResponse.FirstOrDefault().HitsContainers.FirstOrDefault().Hits.Select(a => _mapper.Map<Models.Graph.SearchHit>(a))
            .ToHtmlTable(string.IsNullOrEmpty(skipToken) ? (0 + StringExtensions.PageSize).ToString() : (int.Parse(skipToken) + StringExtensions.PageSize).ToString());
        }

    }
}