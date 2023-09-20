using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {
        [MethodDescription("HRM|Fetches all leave types.")]
        public async Task<IEnumerable<LeaveType>> GetAllLeaveTypes()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("hrm/leavetype");

            var result = await response.FromJson<SimplicateDataRequest<LeaveType>>();
            return result.Data;


        }

        [MethodDescription("HRM|Search for employees using multiple filters.")]
        public async Task<string> SearchEmployees(
            [ParameterDescription("Employee name.")] string employeeName = null,
            [ParameterDescription("Function.")] string function = null,
            [ParameterDescription("Employment status (e.g., active).")] string employmentStatus = null,
            [ParameterDescription("Created at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(employeeName)) filters["[name]"] = $"*{employeeName}*";
            if (!string.IsNullOrEmpty(function)) filters["[function]"] = $"*{function}*";
            if (!string.IsNullOrEmpty(employmentStatus)) filters["[employment_status]"] = $"*{employmentStatus}*";
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;

            return await FetchSimplicateHtmlData<Employee>(filters, "hrm/employee", pageNumber);
        }



    }
}