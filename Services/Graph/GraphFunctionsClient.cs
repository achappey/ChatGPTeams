using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using AutoMapper;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {
        private readonly string _token;
        private readonly IMapper _mapper;

        public GraphFunctionsClient(string token, IMapper mapper)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;
            _mapper = mapper;
        }

        public Models.Response SuccessResponse()
        {
            return new Models.Response
            {
                Status = "success",
                Message = "The function was executed successfully.",
                Timestamp = DateTime.UtcNow
            };
        }

        [MethodDescription("Get an e-mail by id.")]
        public async Task<Models.Graph.Email> GetMail(
            [ParameterDescription("The ID of the e-mail.")] string id)
        {
            var graphClient = GetAuthenticatedClient();
            var message = await graphClient.Me.Messages[id].Request().GetAsync();

            return this._mapper.Map<Models.Graph.Email>(message);
        }

        // Search for groups based on group name or description.
        [MethodDescription("Searches for groups based on name or description.")]
        public async Task<IEnumerable<Models.Graph.Group>> SearchGroups(
            [ParameterDescription("The group name to filter on.")] string name = null,
            [ParameterDescription("The description to filter on.")] string description = null)
        {
            var graphClient = GetAuthenticatedClient();

            string searchQuery = null;

            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(description))
            {
                searchQuery = $"\"displayName:{name ?? "*"}\" OR \"description:{description ?? "*"}\"";
            }

            var filterOptions = new List<QueryOption>();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                filterOptions.Add(new QueryOption("$search", searchQuery));
            }

            var groups = await graphClient.Groups.Request(filterOptions)
                        .Header("ConsistencyLevel", "eventual")
                        .GetAsync();

            return groups.Select(t => _mapper.Map<Models.Graph.Group>(t));
        }




        // // Gets the user's photo
        // public async Task<PhotoResponse> GetPhotoAsync()
        // {
        //     HttpClient client = new HttpClient();
        //     client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
        //     client.DefaultRequestHeaders.Add("Accept", "application/json");

        //     using (var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value"))
        //     {
        //         if (!response.IsSuccessStatusCode)
        //         {
        //             throw new HttpRequestException($"Graph returned an invalid success code: {response.StatusCode}");
        //         }

        //         var stream = await response.Content.ReadAsStreamAsync();
        //         var bytes = new byte[stream.Length];
        //         stream.Read(bytes, 0, (int)stream.Length);

        //         var photoResponse = new PhotoResponse
        //         {
        //             Bytes = bytes,
        //             ContentType = response.Content.Headers.ContentType?.ToString(),
        //         };

        //         if (photoResponse != null)
        //         {
        //             photoResponse.Base64String = $"data:{photoResponse.ContentType};base64," +
        //                                          Convert.ToBase64String(photoResponse.Bytes);
        //         }

        //         return photoResponse;
        //     }
        // }

        // Get an Authenticated Microsoft Graph client using the token issued to the user.
        private GraphServiceClient GetAuthenticatedClient()
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));
            return graphClient;
        }
    }


}