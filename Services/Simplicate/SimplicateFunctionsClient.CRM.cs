using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models.Simplicate;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {

        [MethodDescription("CRM|Adds a new organization to Simplicate.")]
        public async Task<Models.Response> AddNewOrganization(
            [ParameterDescription("The name of the organization.")] string name,
            [ParameterDescription("The email of the organization.")] string email = null,
            [ParameterDescription("The linkedin url of the organization.")] string linkedin = null,
            [ParameterDescription("The website url of the organization.")] string website = null,
            [ParameterDescription("The industry id of the organization.")] string industryId = null,
            [ParameterDescription("A note to add to the organization.")] string note = null,
            [ParameterDescription("The phone number of the organization.")] string phone = null,
            [ParameterDescription("The person id to be linked to the organization.")] string personId = null)
        {
            var client = await GetAuthenticatedHttpClient();
            var linkedPersons = new List<object>();

            if (!string.IsNullOrEmpty(personId))
            {
                linkedPersons.Add(new
                {
                    person_id = personId
                });
            }

            var orgData = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(name)) orgData["name"] = name;
            if (!string.IsNullOrEmpty(email)) orgData["email"] = email;
            if (!string.IsNullOrEmpty(phone)) orgData["phone"] = phone;
            if (!string.IsNullOrEmpty(note)) orgData["note"] = note;
            if (!string.IsNullOrEmpty(industryId)) orgData["industry"] = new { id = industryId };
            if (linkedPersons.Any()) orgData["linked_persons_contacts"] = linkedPersons;

            var response = await client.PostAsync("crm/organization", PrepareJsonContent(orgData));

            if (response.IsSuccessStatusCode)
            {
                return SuccessResponse();
            }

            throw new Exception(response.ReasonPhrase);
        }

        [MethodDescription("CRM|Adds a new person to Simplicate.")]
        public async Task<Models.Response> AddNewPerson(
            [ParameterDescription("The family name of the person.")] string familyName,
            [ParameterDescription("The full name of the person.")] string fullName,
            [ParameterDescription("The first name of the person.")] string firstName = null,
            [ParameterDescription("The job title of the person.")] string jobTitle = null,
            [ParameterDescription("The email of the person.")] string email = null,
            [ParameterDescription("The mobile phone number of the person.")] string mobilePhone = null,
            [ParameterDescription("The work phone number of the person.")] string workPhone = null,
            [ParameterDescription("A note to add to the person.")] string note = null,
            [ParameterDescription("The organization id to be linked to the person.")] string organizationId = null)
        {
            var client = await GetAuthenticatedHttpClient();

            var linkedOrganizations = new List<object>();

            if (!string.IsNullOrEmpty(organizationId))
            {
                var linkedOrganizationData = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(organizationId)) linkedOrganizationData["organization_id"] = organizationId;
                if (!string.IsNullOrEmpty(workPhone)) linkedOrganizationData["work_mobile"] = workPhone;
                if (!string.IsNullOrEmpty(jobTitle)) linkedOrganizationData["work_function"] = jobTitle;
                if (!string.IsNullOrEmpty(email)) linkedOrganizationData["work_email"] = email;

                if (linkedOrganizationData.Any())
                {
                    linkedOrganizations.Add(linkedOrganizationData);
                }
            }

            var personData = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(firstName)) personData["first_name"] = firstName;
            if (!string.IsNullOrEmpty(familyName)) personData["family_name"] = familyName;
            if (!string.IsNullOrEmpty(fullName)) personData["full_name"] = fullName;
            if (!string.IsNullOrEmpty(email)) personData["email"] = email;
            if (linkedOrganizations.Any()) personData["linked_as_contact_to_organization"] = linkedOrganizations;
            if (!string.IsNullOrEmpty(mobilePhone)) personData["phone"] = mobilePhone;
            if (!string.IsNullOrEmpty(note)) personData["note"] = note;

            var response = await client.PostAsync("crm/person", PrepareJsonContent(personData));

            if (response.IsSuccessStatusCode)
            {
                return SuccessResponse();
            }

            throw new Exception(response.ReasonPhrase);
        }

        [MethodDescription("CRM|Search for persons using multiple filters.")]
        public async Task<string> SearchPersons(
            [ParameterDescription("The first name of the person.")] string firstName = null,
            [ParameterDescription("The family name of the person.")] string familyName = null,
            [ParameterDescription("The email of the person.")] string email = null,
            [ParameterDescription("The name of the relation manager of the person.")] string relationManager = null,
            [ParameterDescription("Created at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
            [ParameterDescription("The phone number of the person.")] string phone = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(firstName)) filters["[first_name]"] = $"*{firstName}*";
            if (!string.IsNullOrEmpty(familyName)) filters["[family_name]"] = $"*{familyName}*";
            if (!string.IsNullOrEmpty(email)) filters["[email]"] = $"*{email}*";
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;
            if (!string.IsNullOrEmpty(phone)) filters["[phone]"] = $"*{phone}*";
            if (!string.IsNullOrEmpty(relationManager)) filters["[relation_manager.name]"] = $"*{relationManager}*";

            return await FetchSimplicateHtmlData<Person>(filters, "crm/person", pageNumber);
        }


        [MethodDescription("CRM|Search for organizations using multiple filters.")]
        public async Task<string> SearchOrganizations(
            [ParameterDescription("The name of the organization.")] string name = null,
            [ParameterDescription("The email of the organization.")] string email = null,
            [ParameterDescription("The phone number of the organization.")] string phone = null,
            [ParameterDescription("The industry of the organization.")] string industry = null,
            [ParameterDescription("The relation type of the organization.")] string relationType = null,
            [ParameterDescription("Created at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
            [ParameterDescription("The relation manager of the organization.")] string relationManager = null,
            [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(name)) filters["[name]"] = $"*{name}*";
            if (!string.IsNullOrEmpty(email)) filters["[email]"] = $"*{email}*";
            if (!string.IsNullOrEmpty(phone)) filters["[phone]"] = $"*{phone}*";
            if (!string.IsNullOrEmpty(industry)) filters["[industry.name]"] = $"*{industry}*";
            if (!string.IsNullOrEmpty(relationType)) filters["[relation_type.label]"] = $"*{relationType}*";
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;
            if (!string.IsNullOrEmpty(relationManager)) filters["[relation_manager.name]"] = $"*{relationManager}*";

            return await FetchSimplicateHtmlData<Organization>(filters, "crm/organization", pageNumber);

        }

        [MethodDescription("CRM|Search for contact persons using multiple filters.")]
        public async Task<string> SearchContactPersons(
                    [ParameterDescription("The full name of the contact person.")] string fullName = null,
                    [ParameterDescription("The organization name of the contact person.")] string organizationName = null,
                    [ParameterDescription("The work email of the contact person.")] string workEmail = null,
                    [ParameterDescription("The work function of the contact person.")] string workFunction = null,
                    [ParameterDescription("Created at or after this date and time (format: yyyy-MM-dd HH:mm:ss).")] string createdAfter = null,
                    [ParameterDescription("The work phone of the contact person.")] string workPhone = null,
                    [ParameterDescription("The work mobile of the contact person.")] string workMobile = null,
                    [ParameterDescription("The page number.")] long pageNumber = 1)
        {
            createdAfter?.EnsureValidDateFormat();

            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(fullName)) filters["[person.full_name]"] = $"*{fullName}*";
            if (!string.IsNullOrEmpty(organizationName)) filters["[organization.name]"] = $"*{organizationName}*";
            if (!string.IsNullOrEmpty(workEmail)) filters["[work_email]"] = $"*{workEmail}*";
            if (!string.IsNullOrEmpty(workFunction)) filters["[work_function]"] = $"*{workFunction}*";
            if (!string.IsNullOrEmpty(createdAfter)) filters["[created_at][ge]"] = createdAfter;
            if (!string.IsNullOrEmpty(workPhone)) filters["[work_phone]"] = $"*{workPhone}*";
            if (!string.IsNullOrEmpty(workMobile)) filters["[work_mobile]"] = $"*{workMobile}*";

            return await FetchSimplicateHtmlData<Organization>(filters, "crm/contactperson", pageNumber);
        }

        [MethodDescription("CRM|Gets all my organization profiles.")]
        public async Task<IEnumerable<MyOrganizationProfile>> GetAllMyOrganizations()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("crm/myorganizationprofile");

            var result = await response.FromJson<SimplicateDataRequest<MyOrganizationProfile>>();
            return result.Data;
        }

        [MethodDescription("CRM|Gets all relation types.")]
        public async Task<IEnumerable<RelationType>> GetAllRelationTypes()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("crm/relationtype");

            var result = await response.FromJson<SimplicateDataRequest<RelationType>>();
            return result.Data;
        }

        [MethodDescription("CRM|Gets all industry types.")]
        public async Task<IEnumerable<Industry>> GetAllIndustries()
        {
            var client = await GetAuthenticatedHttpClient();
            var response = await client.GetAsync("crm/industry");

            var result = await response.FromJson<SimplicateDataRequest<Industry>>();
            return result.Data;
        }
    }
}