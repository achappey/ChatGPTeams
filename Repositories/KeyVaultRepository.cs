using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace achappey.ChatGPTeams.Repositories;

public interface IKeyVaultRepository
{
    Task<KeyVaultSecret> GetSecret(string vault, string name);
    Task<IEnumerable<string>> GetSecrets(string vault);
    Task<string> CreateSecret(string vault, string name, string value, string contentType);
    Task UpdateSecret(string vault, string name, string newValue);
    Task DeleteSecret(string vault, string name);
}

public class KeyVaultRepository : IKeyVaultRepository
{
    private readonly ILogger<KeyVaultRepository> _logger;
    private readonly AppConfig _appConfig;

    public KeyVaultRepository(ILogger<KeyVaultRepository> logger, AppConfig appConfig)
    {
        _logger = logger;
        _appConfig = appConfig;
    }

    private SecretClient GetSecretClient(string vault)
    {
        var kvUri = $"https://{vault}.vault.azure.net";

        return new SecretClient(new Uri(kvUri), GetAccessTokenCredential());
    }

    public AccessTokenCredential GetAccessTokenCredential()
    {
        string authority = $"https://login.microsoftonline.com/{_appConfig.MicrosoftAppTenantId}";

        var app = ConfidentialClientApplicationBuilder.Create(_appConfig.MicrosoftAppId)
            .WithClientSecret(_appConfig.MicrosoftAppPassword)
            .WithAuthority(new Uri(authority))
            .Build();

        string[] scopes = new List<string>() { "https://vault.azure.net/.default" }.ToArray();

        // Note that this is blocking the async call, consider restructuring if needed
        AuthenticationResult result = app.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();

        return new AccessTokenCredential(result.AccessToken);
    }

    public async Task<KeyVaultSecret> GetSecret(string vault, string name)
    {
        var client = GetSecretClient(vault);
        var secret = await client.GetSecretAsync(name);
        return secret.Value;

    }

    public async Task<string> CreateSecret(string vault, string name, string value, string contentType)
    {
        var client = GetSecretClient(vault);
        var result = await client.SetSecretAsync(name, value);
        result.Value.Properties.ContentType = contentType;

        await client.UpdateSecretPropertiesAsync(result.Value.Properties);

        return result.Value.Id.ToString();
    }

    public async Task UpdateSecret(string vault, string name, string newValue)
    {
        var client = GetSecretClient(vault);

        var currentSecret = await client.GetSecretAsync(name);

        if (currentSecret == null)
        {
            throw new KeyNotFoundException();
        }

        var updated = await client.SetSecretAsync(name, newValue);

        updated.Value.Properties.ContentType = currentSecret.Value.Properties.ContentType;

        await client.UpdateSecretPropertiesAsync(updated.Value.Properties);
    }

    public async Task DeleteSecret(string vault, string name)
    {
        var client = GetSecretClient(vault);

        await client.StartDeleteSecretAsync(name);
    }


    public async Task<IEnumerable<string>> GetSecrets(string vault)
    {
        var client = GetSecretClient(vault);

        var results = new List<string>();

        await foreach (SecretProperties secretProperties in client.GetPropertiesOfSecretsAsync())
        {
            results.Add(secretProperties.Name);
        }

        return results;
    }


}
