using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Models.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Searches for your Planner tasks based on title or description.")]
        public async Task<IEnumerable<PlannerTask>> SearchMyPlannerTasks(
            [ParameterDescription("The task title to filter on.")] string title = null,
            [ParameterDescription("The description to filter on.")] string description = null)
        {
            var graphClient = GetAuthenticatedClient();

            var tasks = await graphClient.Me.Planner.Tasks
                                .Request()
                                .GetAsync();

            var filteredTasks = tasks.Where(task =>
                (string.IsNullOrEmpty(title) || task.Title.ToLower().Contains(title.ToLower())) &&
                (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(task.Details.Description) || task.Details.Description.ToLower().Contains(description.ToLower()))
            );

            return filteredTasks.Select(t => _mapper.Map<PlannerTask>(t));
        }

    }
}