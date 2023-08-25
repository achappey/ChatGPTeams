using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Searches across SharePoint and OneDrive resources.")]
        public async Task<IEnumerable<Models.Graph.SearchHit>> SearchDriveContent(
            [ParameterDescription("The search query.")] string query)
        {
            return await SearchContent(query, EntityType.DriveItem);
        }

        [MethodDescription("Searches Outlook messages.")]
        public async Task<IEnumerable<Models.Graph.SearchHit>> SearchOutlookContent(
                    [ParameterDescription("The search query.")] string query)
        {
            return await SearchContent(query, EntityType.Message);
        }

        [MethodDescription("Searches chat messages.")]
        public async Task<IEnumerable<Models.Graph.SearchHit>> SearchChatContent(
                          [ParameterDescription("The search query.")] string query)
        {
            return await SearchContent(query, EntityType.ChatMessage);
        }

        private async Task<IEnumerable<Models.Graph.SearchHit>> SearchContent(
                          string query, EntityType type)
        {
            var graphClient = GetAuthenticatedClient();

            if (string.IsNullOrEmpty(query))
            {
                query = "*";
            }

            var searchRequest = new SearchRequestObject()
            {
                Query = new SearchQuery
                {
                    QueryString = query,
                },
                EntityTypes = new List<EntityType> { type }
            };

            var searchResponse = await graphClient.Search
                .Query(new List<SearchRequestObject>() {
                    searchRequest
                })
                .Request()
                .Top(10)
                .PostAsync();

            return searchResponse.FirstOrDefault().HitsContainers.FirstOrDefault().Hits.Select(a => _mapper.Map<Models.Graph.SearchHit>(a));
        }


    }
}