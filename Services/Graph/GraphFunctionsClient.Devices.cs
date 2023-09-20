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

        [MethodDescription("Devices|Gets devices for the specified criteria.")]
        public async Task<string> SearchDevices(
        //       [ParameterDescription("Device name to search for")] string deviceName = null,
               [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();
            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var devices = await graphClient.Devices
                .Request(filterOptions)
                .Top(10)
                .GetAsync();

            return devices.CurrentPage.Select(_mapper.Map<Models.Graph.Device>).ToHtmlTable(devices.NextPageRequest?.QueryOptions.FirstOrDefault(a => a.Name == "$skiptoken")?.Value);
        }

        [MethodDescription("Devices|Gets managed devices for the specified criteria.")]
        public async Task<string> SearchManagedDevices(
                    //       [ParameterDescription("Device name to search for")] string deviceName = null,
                    [ParameterDescription("The next page skip token.")] string skipToken = null)
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();
            if (!string.IsNullOrEmpty(skipToken))
            {
                filterOptions.Add(new QueryOption("$skiptoken", skipToken));
            }

            var devices = await graphClient.DeviceManagement.ManagedDevices
                .Request(filterOptions)
                .Top(10)
                .GetAsync();

            return devices.CurrentPage.Select(_mapper.Map<Models.Graph.ManagedDevice>).ToHtmlTable(devices.NextPageRequest?.QueryOptions.FirstOrDefault(a => a.Name == "$skiptoken")?.Value);
        }
        
        [MethodDescription("Devices|Gets managed corporate devices for the specified criteria.")]
        public async Task<string> SearchManagedCorporateDevices(
                           //       [ParameterDescription("Device name to search for")] string deviceName = null,
                           )
        {
            var graphClient = GetAuthenticatedClient();

            var filterOptions = new List<QueryOption>();

            var devices = await graphClient.DeviceManagement.ManagedDevices
                .Request(filterOptions)
                .Top(999)
                .GetAsync();

            return devices.CurrentPage.Where(a => a.ManagedDeviceOwnerType == ManagedDeviceOwnerType.Company)
                .Select(_mapper.Map<Models.Graph.ManagedDevice>).ToHtmlTable(null);
        }

    }
}