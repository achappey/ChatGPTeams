

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace achappey.ChatGPTeams;


public class AppConfig
{
    public string MicrosoftAppId { get; set; } = null!;
    public string MicrosoftAppPassword { get; set; } = null!;
    public string MicrosoftAppTenantId { get; set; } = null!;
    public string OpenAI { get; set; } = null!;
    public string ConnectionName { get; set; } = null!;
    public string Database { get; set; } = null!;
    public string SharePointSiteId { get; set; } = null!;
}

public class AccessTokenCredential : TokenCredential
{
    private readonly string _accessToken;

    public AccessTokenCredential(string accessToken)
    {
        _accessToken = accessToken;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}
