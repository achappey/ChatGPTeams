using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Repositories;

public interface IDataverseRepository
{
    Task<IEnumerable<string>> GetEntityDefinitions(string vault);
}

public class DataverseRepository : IDataverseRepository
{
    private readonly ILogger<KeyVaultRepository> _logger;
    private readonly AppConfig _appConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataverseClientFactory _dataverseClientFactory;


    public DataverseRepository(ILogger<KeyVaultRepository> logger, AppConfig appConfig, 
    IHttpClientFactory httpClientFactory, IDataverseClientFactory dataverseClientFactory)
    {
        _logger = logger;
        _appConfig = appConfig;
        _dataverseClientFactory = dataverseClientFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<string>> GetEntityDefinitions(string name)
    {
        var client = _dataverseClientFactory.GetDataverseClient(name);
        var result = await client.GetAsync("WhoAmI");
        
        if (result.IsSuccessStatusCode)
        {
            var content = await result.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<IEnumerable<string>>(content);
            return items;
        }
        
        throw new Exception(result.ReasonPhrase);

    }



}
