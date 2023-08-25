using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Models.Graph;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Searches for sites based on keywords.")]
        public async Task<IEnumerable<Models.Graph.Site>> SearchSites([ParameterDescription("The query to search on.")] string query = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();

            if (!string.IsNullOrEmpty(query))
            {
                filterOptions.Add(new QueryOption("search", query));
            }

            var sites = await graphClient.Sites
                .Request(filterOptions)
                .Header("ConsistencyLevel", "eventual")
                .GetAsync();

            return sites.Select(_mapper.Map<Models.Graph.Site>);
        }


    }
}