using System;
using System.Net.Http.Headers;
using achappey.ChatGPTeams.Services.Graph;
using AutoMapper;
using Microsoft.Graph;

public interface IGraphClientFactory
{
    GraphServiceClient Create();
    GraphFunctionsClient GetFunctionsClient();
}

public class GraphClientFactory : IGraphClientFactory
{
    private readonly ITokenService _tokenService;
    private readonly GraphServiceClient _graphServiceClient;
    private readonly IMapper _mapper;

    public GraphClientFactory(ITokenService tokenService, IMapper mapper)
    {
        _tokenService = tokenService;
        _mapper = mapper;

        _graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(requestMessage =>
        {
            // Get the access token.
            string token = _tokenService.GetToken();

            // Append the access token to the request.
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

            // Get event times in the current time zone.
            requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");
            return System.Threading.Tasks.Task.CompletedTask;
        }));

    }

    public GraphServiceClient Create()
    {
        return _graphServiceClient;
    }

    public GraphFunctionsClient GetFunctionsClient()
    {
        return new GraphFunctionsClient(_tokenService.GetToken(), _mapper);
    }


}
