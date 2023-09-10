using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Graph;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Me|Gets trending documents for the current user.")]
        public async Task<string> GetMyTrendingDocuments(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();
            if (resourceType.HasValue)
            {
                filterOptions.Add(new QueryOption("$filter", $"ResourceVisualization/Type eq '{resourceType.Value}'"));
            }

            var insights = await graphClient.Me.Insights.Trending
                                .Request(filterOptions)
                                .GetAsync();

            return insights.CurrentPage.Select(_mapper.Map<Models.Graph.Trending>).ToHtmlTable(null);
        }

        [MethodDescription("Me|Gets used documents for the current user.")]
        public async Task<string> GetMyUsedDocuments(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var trendingRequest = graphClient.Me.Insights.Used.Request().Top(10);

            if (resourceType.HasValue)
            {
                trendingRequest = trendingRequest.Filter($"ResourceVisualization/Type eq '{resourceType.Value}'");
            }

            var insights = await trendingRequest.GetAsync();

              return insights.CurrentPage.Select(_mapper.Map<Models.Graph.UsedInsight>).ToHtmlTable(null);
        }

        [MethodDescription("Me|Gets documents shared with the current user.")]
        public async Task<string> GetDocumentsSharedWithMe(
            [ParameterDescription("The type of the resource.")] ResourceType? resourceType = null)
        {
            var graphClient = GetAuthenticatedClient();

            var sharedRequest = graphClient.Me.Insights.Shared.Request().Top(10);

            if (resourceType.HasValue)
            {
                sharedRequest = sharedRequest.Filter($"ResourceVisualization/Type eq '{resourceType.Value}'");
            }

            var sharedItems = await sharedRequest.GetAsync();

            return sharedItems.CurrentPage.Select(_mapper.Map<Models.Graph.SharedInsight>).ToHtmlTable(null);
        }

    }
}