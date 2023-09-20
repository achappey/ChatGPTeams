using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {
        [MethodDescription("Hours|Search for hours using multiple filters.")]
        public async Task<string> SearchHours(
                [ParameterDescription("Employee name.")] string employeeName = null,
                [ParameterDescription("Project name.")] string projectName = null,
                [ParameterDescription("Startdate at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateAfter = null,
                [ParameterDescription("Startdate at or before this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateBefore = null,
                [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            startDateAfter?.EnsureValidDateFormat();
            startDateBefore?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(employeeName)) filters["[employee.name]"] = $"*{employeeName}*";
            if (!string.IsNullOrEmpty(projectName)) filters["[project.name]"] = $"*{projectName}*";
            if (!string.IsNullOrEmpty(startDateAfter)) filters["[start_date][ge]"] = startDateAfter;
            if (!string.IsNullOrEmpty(startDateBefore)) filters["[start_date][le]"] = startDateBefore;

            return await FetchSimplicateHtmlData<Hour>(filters, "hours/hours", pageNumber);
        }

        [MethodDescription("Hours|Gets hours per employee using multiple filters.")]
        public async Task<IEnumerable<dynamic>> GetHoursPerEmployee(
              [ParameterDescription("Employee name.")] string employeeName = null,
              [ParameterDescription("Project name.")] string projectName = null,
              [ParameterDescription("Status.")] HourStatus? status = null,
              [ParameterDescription("Startdate at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateAfter = null,
              [ParameterDescription("Startdate at or before this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateBefore = null)
        {
            startDateAfter?.EnsureValidDateFormat();
            startDateBefore?.EnsureValidDateFormat();

            var client = await GetAuthenticatedHttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(employeeName)) queryString["q[employee.name]"] = $"*{employeeName}*";
            if (!string.IsNullOrEmpty(projectName)) queryString["q[project.name]"] = $"*{projectName}*";
            if (!string.IsNullOrEmpty(startDateAfter)) queryString["q[start_date][ge]"] = startDateAfter;
            if (!string.IsNullOrEmpty(startDateBefore)) queryString["q[start_date][le]"] = startDateBefore;

            var response = await client.PagedRequest<Hour>($"hours/hours?{queryString}");

            var groupedHours = response
                .Where(a => !status.HasValue || (status.HasValue && a.Status == StringExtensions.GetEnumMemberAttributeValue(status.Value)))
                .GroupBy(h => new { h.Employee.Id, h.Employee.Name })
                .Select(g => new
                {
                    EmployeeName = g.Key.Name,
                    Status = status.HasValue ? StringExtensions.GetEnumMemberAttributeValue(status.Value) : g.First().Status,
                    TotalHours = g.Sum(h => h.Hours)
                })
                .Where(y => y.TotalHours > 0);

            return groupedHours;

        }

        [MethodDescription("Hours|Gets hours per project using multiple filters.")]
        public async Task<IEnumerable<dynamic>> GetHoursPerProject(
              [ParameterDescription("Employee name.")] string employeeName = null,
              [ParameterDescription("Project name.")] string projectName = null,
              [ParameterDescription("Status.")] HourStatus? status = null,
              [ParameterDescription("Startdate at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateAfter = null,
              [ParameterDescription("Startdate at or before this date and time (format: yyyy-MM-dd HH:mm:ss).")] string startDateBefore = null)
        {
            startDateAfter?.EnsureValidDateFormat();
            startDateBefore?.EnsureValidDateFormat();

            var client = await GetAuthenticatedHttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(employeeName)) queryString["q[employee.name]"] = $"*{employeeName}*";
            if (!string.IsNullOrEmpty(projectName)) queryString["q[project.name]"] = $"*{projectName}*";
            if (!string.IsNullOrEmpty(startDateAfter)) queryString["q[start_date][ge]"] = startDateAfter;
            if (!string.IsNullOrEmpty(startDateBefore)) queryString["q[start_date][le]"] = startDateBefore;

            var response = await client.PagedRequest<Hour>($"hours/hours?{queryString}");

            var groupedHours = response
                .Where(a => !status.HasValue || (status.HasValue && a.Status == StringExtensions.GetEnumMemberAttributeValue(status.Value)))
                .GroupBy(h => new { h.Project.Id, h.Project.Name, h.Status })
                .Select(g => new
                {
                    ProjectName = g.Key.Name,
                    Status = status.HasValue ? StringExtensions.GetEnumMemberAttributeValue(status.Value) : g.First().Status,
                    TotalHours = g.Sum(h => h.Hours)
                })
                .Where(y => y.TotalHours > 0);

            return groupedHours;

        }

        [MethodDescription("Hours|Add a new hours registration.")]
        public async Task<Models.Response> AddNewHour(
            [ParameterDescription("Employee ID.")] string employeeId,
            [ParameterDescription("Project ID.")] string projectId,
            [ParameterDescription("Project Service ID.")] string projectServiceId,
            [ParameterDescription("Start Date.")] string startDate,
            [ParameterDescription("End Date.")] string endDate,
            [ParameterDescription("The number of hours.")] double hours,
            [ParameterDescription("Note.")] string note = null)
        {
            startDate?.EnsureValidDateFormat();
            endDate?.EnsureValidDateFormat();

            var client = await GetAuthenticatedHttpClient();

            var body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(employeeId)) body["employee_id"] = employeeId;
            if (!string.IsNullOrEmpty(projectId)) body["project_id"] = projectId;
            if (!string.IsNullOrEmpty(projectServiceId)) body["projectservice_id"] = projectServiceId;
            if (startDate != null) body["start_date"] = startDate;
            if (endDate != null) body["end_date"] = endDate;
            if (!string.IsNullOrEmpty(note)) body["note"] = note;
            body["hours"] = hours;

            var response = await client.PostAsync("hours/hours", PrepareJsonContent(body));

            if (response.IsSuccessStatusCode)
            {
                return SuccessResponse();
            }

            throw new Exception(response.ReasonPhrase);
        }

        [MethodDescription("Hours|Fetches all hours types.")]
        public async Task<IEnumerable<HoursType>> GetAllHoursTypes()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("hours/hourstype");

            var result = await response.FromJson<SimplicateDataRequest<HoursType>>();
            return result.Data;
        }
    }
}