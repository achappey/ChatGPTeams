using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Graph;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("SharePoint|Searches for sites based on keywords.")]
        public async Task<string> SearchSites([ParameterDescription("The query to search on.")] string query = null,
                                      [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();

            if (!string.IsNullOrEmpty(query))
            {
                filterOptions.Add(new QueryOption("search", query));
            }

            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var sites = await graphClient.Sites
                .Request(filterOptions)
                .Top(10)
                .Header("ConsistencyLevel", "eventual")
                .GetAsync();

            return sites.CurrentPage.Select(_mapper.Map<Models.Graph.Site>).ToHtmlTable(sites.NextPageRequest?.QueryOptions.FirstOrDefault(a => a.Name == "$skiptoken")?.Value);
        }



    }
}