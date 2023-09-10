using System;
using System.Net.Http;
using System.Net.Http.Headers;
using AutoMapper;

public interface IDataverseClientFactory
{
    HttpClient GetDataverseClient(string name);
}

public class DataverseClientFactory : IDataverseClientFactory
{
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IHttpClientFactory _httpClientFactory;

    public DataverseClientFactory(ITokenService tokenService, IMapper mapper, IHttpClientFactory httpClientFactory)
    {
        _tokenService = tokenService;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;


    }

    public HttpClient GetDataverseClient(string name)
    {
        return GetAuthenticatedHttpClient(name);
    }

    private HttpClient GetAuthenticatedHttpClient(string name)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri($"https://{name}.crm4.dynamics.com/" + "api/data/v9.2/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.GetDataverseToken());
        httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
        httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return httpClient;
    }


}
