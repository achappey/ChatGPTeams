using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Retrieves the pages for a specific site.")]
        public async Task<IEnumerable<Models.Graph.Page>> GetSitePages(
            [ParameterDescription("The ID of the site.")] string siteId,
            [ParameterDescription("The name to filter on.")] string name = null)
        {
            var graphClient = GetAuthenticatedClient();
            string filter = null;

            if (!string.IsNullOrEmpty(name))
            {
                filter = $"contains(name, '{name}')";
            }

            var pages = await graphClient.Sites[siteId].Pages
                        .Request()
                        .Filter(filter)
                        .GetAsync();

            return pages.Select(_mapper.Map<Models.Graph.Page>);
        }


    }
}