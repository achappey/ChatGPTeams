using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;

public interface IVaultService
{
    Task<IEnumerable<Vault>> GetMyVaultsAsync();
    Task<IEnumerable<string>> GetSecretsAsync(string vault);
    Task<string> CreateSecret(string vault, string name, string value, string contentType);
    Task UpdateSecret(string vault, string name, string value);
    Task DeleteSecret(string vault, string name);
    Task SendSecretAsync(string vault, string name);
}

public class VaultService : IVaultService
{
    private readonly IVaultRepository _vaultRepository;
    private readonly IKeyVaultRepository _keyVaultRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGraphClientFactory _graphClientFactory;

    public VaultService(IVaultRepository vaultRepository, IGraphClientFactory graphClientFactory,
        IKeyVaultRepository keyVaultRepository, IUserRepository userRepository)
    {
        _vaultRepository = vaultRepository;
        _keyVaultRepository = keyVaultRepository;
        _userRepository = userRepository;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Vault>> GetMyVaultsAsync()
    {
        var vaults = await _vaultRepository.GetAll();
        var user = await _userRepository.GetCurrent();
        var result = new List<Vault>();

        foreach (var vault in vaults)
        {
            if (await IsOwner(user, vault) || await IsReader(user, vault))
            {
                result.Add(vault);
            }
        }

        return result;
    }

    private async Task<Vault> GetMyVaultAsync(string name)
    {
        var vaults = await _vaultRepository.GetAll();
        var user = await _userRepository.GetCurrent();

        foreach (var vault in vaults)
        {
            if (vault.Title.ToLowerInvariant() == name.ToLowerInvariant() && (await IsOwner(user, vault) || await IsReader(user, vault)))
            {
                return vault;
            }
        }

        return null;
    }


    public async Task<string> CreateSecret(string vault, string name, string value, string contentType)
    {
        var myVault = await GetMyVaultAsync(vault);
        if (myVault != null && await IsOwner(await _userRepository.GetCurrent(), myVault))
            return await _keyVaultRepository.CreateSecret(vault, name, value, contentType);

        throw new UnauthorizedAccessException();
    }

    public async Task UpdateSecret(string vault, string name, string value)
    {
        var myVault = await GetMyVaultAsync(vault);
        if (myVault != null && await IsOwner(await _userRepository.GetCurrent(), myVault))
            await _keyVaultRepository.UpdateSecret(vault, name, value);
        else
            throw new UnauthorizedAccessException();
    }

    public async Task DeleteSecret(string vault, string name)
    {
        var myVault = await GetMyVaultAsync(vault);
        if (myVault != null && await IsOwner(await _userRepository.GetCurrent(), myVault))
            await _keyVaultRepository.DeleteSecret(vault, name);
        else
            throw new UnauthorizedAccessException();
    }

    private async Task<bool> IsOwner(User currentUser, Vault vault)
    {
        return await CheckAccess(currentUser, vault.Owners);
    }

    private async Task<bool> IsReader(User currentUser, Vault vault)
    {
        return await CheckAccess(currentUser, vault.Readers);
    }

    private async Task<bool> CheckAccess(User currentUser, IEnumerable<User> users)
    {
        var allUsers = await _userRepository.GetAll();
        var filteredUsers = users.SelectMany(z => allUsers.Where(a => a.Id == z.Id));
        if (filteredUsers.Any(u => u.Id == currentUser.Id)) return true;

        var groups = filteredUsers.Where(t => t.ContentType == "DomainGroup");
        foreach (var group in groups)
        {
            if (await _userRepository.IsMemberOf(group.Mail)) return true;
        }

        return false;
    }

    public async Task SendSecretAsync(string vault, string name)
    {
        var myVault = await GetMyVaultAsync(vault);

        if (myVault != null)
        {
            var secret = await _keyVaultRepository.GetSecret(vault, name);
            await SendEmailAsync(secret.Name, secret.Properties.ContentType);
            await SendEmailAsync(secret.Name, secret.Value);

        }
        else
        {
            throw new UnauthorizedAccessException();
        }


    }

    public async Task<IEnumerable<string>> GetSecretsAsync(string vault)
    {
        var myVault = await GetMyVaultAsync(vault);
        if (myVault != null) return await _keyVaultRepository.GetSecrets(vault);
        throw new UnauthorizedAccessException();
    }

    public async Task SendEmailAsync(string subject, string messageContent)
    {
        var currentUser = await _userRepository.GetCurrent();

        var email = new Microsoft.Graph.Message
        {
            Subject = subject,
            Body = new Microsoft.Graph.ItemBody
            {
                ContentType = Microsoft.Graph.BodyType.Text,
                Content = messageContent
            },
            ToRecipients = new List<Microsoft.Graph.Recipient>
            {
                new Microsoft.Graph.Recipient
                {
                    EmailAddress = new Microsoft.Graph.EmailAddress
                    {
                        Address = currentUser.Mail
                    }
                }
            }
        };

        await GraphService.Me.SendMail(email, null).Request().PostAsync();
    }
}
