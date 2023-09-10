using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {

        [MethodDescription("Projects|Search for projects using multiple filters.")]
        public async Task<string> SearchProjects(
            [ParameterDescription("The project name.")] string projectName = null,
            [ParameterDescription("The project manager's name.")] string projectManager = null,
            [ParameterDescription("Project status label.")] string projectStatusLabel = null,
            [ParameterDescription("Organization name.")] string organizationName = null,
            [ParameterDescription("Created at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
            [ParameterDescription("Project number.")] string projectNumber = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(projectName)) filters["[name]"] = $"*{projectName}*";
            if (!string.IsNullOrEmpty(projectManager)) filters["[project_manager.name]"] = $"*{projectManager}*";
            if (!string.IsNullOrEmpty(projectStatusLabel)) filters["[project_status.label]"] = $"*{projectStatusLabel}*";
            if (!string.IsNullOrEmpty(organizationName)) filters["[organization.name]"] = $"*{organizationName}*";
            if (!string.IsNullOrEmpty(projectNumber)) filters["[project_number]"] = $"*{projectNumber}*";
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;

            return await FetchSimplicateHtmlData<Project>(filters, "projects/project", pageNumber);
        }


    }


}