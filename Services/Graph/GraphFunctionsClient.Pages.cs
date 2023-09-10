using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("SharePoint|Retrieves the pages for a specific site.")]
        public async Task<string> GetSitePages(
            [ParameterDescription("The ID of the site.")] string siteId,
            [ParameterDescription("The name to filter on.")] string name = null,
            [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();
            if (!string.IsNullOrEmpty(name))
            {
                filterOptions.Add(new QueryOption("$filter", $"contains(name, '{name}')"));
            }
            
            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var pages = await graphClient.Sites[siteId].Pages
                        .Request(filterOptions)
                        .GetAsync();

            return pages.CurrentPage.Select(_mapper.Map<Models.Graph.Page>).ToHtmlTable(pages.NextPageRequest?.QueryOptions.FirstOrDefault(a => a.Name == "$skiptoken")?.Value);
        }



    }
}