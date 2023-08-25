using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Models.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Gets trending documents for the current user.")]
        public async Task<IEnumerable<Trending>> GetMyTrendingDocuments(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var trendingRequest = graphClient.Me.Insights.Trending.Request().Top(10);

            if (resourceType.HasValue)
            {
                trendingRequest = trendingRequest.Filter($"ResourceVisualization/Type eq '{resourceType.Value}'");
            }

            var insights = await trendingRequest.GetAsync();

            return insights.Select(_mapper.Map<Trending>);
        }

        [MethodDescription("Gets used documents for the current user.")]
        public async Task<IEnumerable<UsedInsight>> GetMyUsedDocuments(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var trendingRequest = graphClient.Me.Insights.Used.Request().Top(10);

            if (resourceType.HasValue)
            {
                trendingRequest = trendingRequest.Filter($"ResourceVisualization/Type eq '{resourceType.Value}'");
            }

            var insights = await trendingRequest.GetAsync();

            return insights.Select(_mapper.Map<UsedInsight>);
        }

        [MethodDescription("Gets documents shared with the current user.")]
        public async Task<IEnumerable<SharedInsight>> GetDocumentsSharedWithMe(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var sharedRequest = graphClient.Me.Insights.Shared.Request().Top(10);

            if (resourceType.HasValue)
            {
                sharedRequest = sharedRequest.Filter($"ResourceVisualization/Type eq '{resourceType.Value}'");
            }

            var sharedItems = await sharedRequest.GetAsync();

            return sharedItems.Select(_mapper.Map<SharedInsight>);
        }



    }
}