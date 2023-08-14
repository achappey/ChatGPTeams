using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {
        // Search for members based on department, display name, or mail.
        [MethodDescription("Searches for members based on department, display name or mail.")]
        public async Task<IEnumerable<Models.Graph.User>> SearchMembers([ParameterDescription("The department to filter on.")] string department = null,
                                                                        [ParameterDescription("The display name to filter on.")] string displayName = null,
                                                                        [ParameterDescription("The mail to filter on.")] string mail = null)
        {
            var graphClient = GetAuthenticatedClient();
            string searchQuery = null;

            if (!string.IsNullOrEmpty(displayName) || !string.IsNullOrEmpty(mail))
            {
                searchQuery = $"\"displayName:{displayName ?? "*"}\" OR \"mail:{mail ?? "*"}\" OR \"userPrincipalName:{mail ?? "*"}\"";
            }

            string filterQuery = "userType eq 'Member'";
            if (!string.IsNullOrEmpty(department))
            {
                filterQuery += $" and department eq '{department}'";
            }

            var filterOptions = new List<QueryOption>()
            {
                new QueryOption("$filter", filterQuery)
            };

            if (!string.IsNullOrEmpty(searchQuery))
            {
                filterOptions.Add(new QueryOption("$search", searchQuery));
            }

            var users = await graphClient.Users
            .Request(filterOptions)
            .Top(10)
            .Header("ConsistencyLevel", "eventual")
            .GetAsync();

            return users.CurrentPage.Select(u => _mapper.Map<Models.Graph.User>(u));
        }

        // Search for guests based on company name, display name, or mail.
        [MethodDescription("Searches for guest users based on company name, display name or mail.")]
        public async Task<IEnumerable<Models.Graph.User>> SearchGuests([ParameterDescription("The company name to filter on.")] string companyName = null,
                                                                       [ParameterDescription("The display name to filter on.")] string displayName = null,
                                                                       [ParameterDescription("The mail to filter on.")] string mail = null)
        {
            var graphClient = GetAuthenticatedClient();

            string searchQuery = null;

            if (!string.IsNullOrEmpty(displayName) || !string.IsNullOrEmpty(mail))
            {
                searchQuery = $"\"displayName:{displayName ?? "*"}\" OR \"mail:{mail ?? "*"}\" OR \"userPrincipalName:{mail ?? "*"}\"";
            }

            string filterQuery = "userType eq 'Member'";
            if (!string.IsNullOrEmpty(companyName))
            {
                filterQuery += $" and startsWith(companyName, '{companyName}')";
            }

            var filterOptions = new List<QueryOption>()
            {
                new QueryOption("$filter", filterQuery)
            };

            if (!string.IsNullOrEmpty(searchQuery))
            {
                filterOptions.Add(new QueryOption("$search", searchQuery));
            }

            var users = await graphClient.Users
            .Request(filterOptions)
            .Top(10)
            .Header("ConsistencyLevel", "eventual")
            .GetAsync();

            return users.CurrentPage.Select(u => this._mapper.Map<Models.Graph.User>(u));
        }


        // Get information about a specific user by their ID.
        [MethodDescription("Gets information about a specific user based on their ID.")]
        public async Task<Models.Graph.User> GetUser(
            [ParameterDescription("The ID of the user.")] string userId)
        {
            var graphClient = GetAuthenticatedClient();
            var user = await graphClient.Users[userId].Request().GetAsync();

            return _mapper.Map<Models.Graph.User>(user);
        }

        [MethodDescription("Creates a user with the specified properties.")]
        public async Task<Models.Graph.User> CreateUser([ParameterDescription("The nickname of the user.")] string nickname,
                                                        [ParameterDescription("The department of the user.")] string department,
                                                        [ParameterDescription("The display name of the user.")] string displayName,
                                                        [ParameterDescription("The given name of the user.")] string givenName,
                                                        [ParameterDescription("The surname of the user.")] string surname,
                                                        [ParameterDescription("The job title of the user.")] string jobTitle,
                                                        [ParameterDescription("The user principal name (email address) of the user.")] string userPrincipalName,
                                                        [ParameterDescription("The password of the user.")] string password)
        {
            var graphClient = GetAuthenticatedClient();

            var user = new User
            {
                MailNickname = nickname,
                Department = department,
                DisplayName = displayName,
                GivenName = givenName,
                Surname = surname,
                JobTitle = jobTitle,
                UserPrincipalName = userPrincipalName,
                AccountEnabled = true,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = password
                }
            };

            var createdUser = await graphClient.Users
                .Request()
                .AddAsync(user);

            return _mapper.Map<Models.Graph.User>(createdUser);
        }

        [MethodDescription("Updates the user's information with the specified properties.")]
        public async Task<Models.Graph.User> UpdateUser([ParameterDescription("The ID of the user to update.")] string userId,
                                                        [ParameterDescription("The nickname of the user.")] string nickname = null,
                                                        [ParameterDescription("The department of the user.")] string department = null,
                                                        [ParameterDescription("The display name of the user.")] string displayName = null,
                                                        [ParameterDescription("The given name of the user.")] string givenName = null,
                                                        [ParameterDescription("The surname of the user.")] string surname = null,
                                                        [ParameterDescription("The job title of the user.")] string jobTitle = null,
                                                        [ParameterDescription("Enable or disable the user's account.")] bool? accountEnabled = null)
        {
            var graphClient = GetAuthenticatedClient();

            var userToUpdate = new User()
            {
            };

            if (!string.IsNullOrEmpty(nickname)) userToUpdate.MailNickname = nickname;
            if (!string.IsNullOrEmpty(department)) userToUpdate.Department = department;
            if (!string.IsNullOrEmpty(displayName)) userToUpdate.DisplayName = displayName;
            if (!string.IsNullOrEmpty(givenName)) userToUpdate.GivenName = givenName;
            if (!string.IsNullOrEmpty(surname)) userToUpdate.Surname = surname;
            if (!string.IsNullOrEmpty(jobTitle)) userToUpdate.JobTitle = jobTitle;
            if (accountEnabled.HasValue) userToUpdate.AccountEnabled = accountEnabled.Value;

            await graphClient.Users[userId]
                .Request()
                .UpdateAsync(userToUpdate);

            var updatedUser = await graphClient.Users[userId]
                .Request()
                .GetAsync();

            return _mapper.Map<Models.Graph.User>(updatedUser);
        }

    }
}