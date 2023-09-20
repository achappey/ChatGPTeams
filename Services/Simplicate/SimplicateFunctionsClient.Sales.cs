using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {

        [MethodDescription("Sales|Search for sales using multiple filters.")]
        public async Task<string> SearchSales(
            [ParameterDescription("The name of the responsible employee.")] string responsibleEmployeeName = null,
            [ParameterDescription("Organization name.")] string organizationName = null,
            [ParameterDescription("Person name.")] string personName = null,
            [ParameterDescription("Sales subject.")] string subject = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(responsibleEmployeeName)) filters["[responsible_employee.name]"] = $"*{responsibleEmployeeName}*";
            if (!string.IsNullOrEmpty(organizationName)) filters["[organization.name]"] = $"*{organizationName}*";
            if (!string.IsNullOrEmpty(personName)) filters["[person.full_name]"] = $"*{personName}*";
            if (!string.IsNullOrEmpty(subject)) filters["[subject]"] = $"*{subject}*";

            return await FetchSimplicateHtmlData<Sales>(filters, "sales/sales", pageNumber);
        }


        [MethodDescription("Sales|Search for quotes using multiple filters.")]
        public async Task<string> SearchQuotes(
            [ParameterDescription("Quote number.")] string quoteNumber = null,
            [ParameterDescription("Status label.")] string statusLabel = null,
            [ParameterDescription("Quote subject.")] string quoteSubject = null,
            [ParameterDescription("Customer reference.")] string customerReference = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(quoteNumber)) filters["[quote_number]"] = $"*{quoteNumber}*";
            if (!string.IsNullOrEmpty(statusLabel)) filters["[quotestatus.label]"] = $"*{statusLabel}*";
            if (!string.IsNullOrEmpty(quoteSubject)) filters["[quote_subject]"] = $"*{quoteSubject}*";
            if (!string.IsNullOrEmpty(customerReference)) filters["[customer_reference]"] = $"*{customerReference}*";

            return await FetchSimplicateHtmlData<Quote>(filters, "sales/quote", pageNumber);
        }


        [MethodDescription("Sales|Fetches all revenue groups.")]
        public async Task<IEnumerable<RevenueGroup>> GetAllRevenueGroups()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("sales/revenuegroup");

            var result = await response.FromJson<SimplicateDataRequest<RevenueGroup>>();
            return result.Data;
        }
    }

}