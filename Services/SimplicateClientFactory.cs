using System;
using System.Net.Http;
using System.Net.Http.Headers;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Services.Graph;
using achappey.ChatGPTeams.Services.Simplicate;
using AutoMapper;
using Microsoft.Graph;

public interface ISimplicateClientFactory
{
    SimplicateFunctionsClient GetFunctionsClient(string userId);
}

public class SimplicateClientFactory : ISimplicateClientFactory
{
    private readonly IKeyVaultRepository _keyVaultRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IMapper _mapper;

    public SimplicateClientFactory(ITokenService tokenService, IMapper mapper, IHttpClientFactory httpClientFactory, IKeyVaultRepository keyVaultRepository)
    {
        _mapper = mapper;
        _keyVaultRepository = keyVaultRepository;
        _httpClientFactory = httpClientFactory;
    }


    public SimplicateFunctionsClient GetFunctionsClient(string userId)
    {
        return new SimplicateFunctionsClient(userId, _mapper, _keyVaultRepository, _httpClientFactory);
    }


}
