using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Repositories;
using AutoMapper;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Services.Simplicate
{
    public partial class SimplicateFunctionsClient
    {
        private readonly string _userId;
        private readonly IMapper _mapper;
        private readonly IKeyVaultRepository _keyVaultRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public SimplicateFunctionsClient(string userId, IMapper mapper, IKeyVaultRepository keyVaultRepository, IHttpClientFactory httpClientFactory)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            _userId = userId;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _keyVaultRepository = keyVaultRepository;
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

        public Models.Response ErrorResponse(string error)
        {
            return new Models.Response
            {
                Status = "exception",
                Message = error,
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<HttpClient> GetAuthenticatedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://fakton.simplicate.nl/api/v2/");
            var secret = await _keyVaultRepository.GetSecret("fakton-simplicate", _userId);

            httpClient.DefaultRequestHeaders.Add("Authentication-Key", secret.Properties.ContentType);
            httpClient.DefaultRequestHeaders.Add("Authentication-Secret", secret.Value);

            return httpClient;
        }

        public StringContent PrepareJsonContent<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<IEnumerable<T>> FetchDataFromSimplicate<T>(
                Dictionary<string, string> filters,
                string endpointUrl)
        {
            // Prepare the client and query string
            var client = await GetAuthenticatedHttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["limit"] = "50";

            // Add the filters to the query string
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryString[$"q{filter.Key}"] = $"{filter.Value}";
                }
            }

            // Make the request
            var response = await client.GetAsync($"{endpointUrl}?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.FromJson<SimplicateDataRequest<T>>();
                return result.Data;

            }

            throw new Exception(response.ReasonPhrase);
        }


        private async Task<SimplicateDataRequest<T>> FetchSimplicateData<T>(
                  Dictionary<string, string> filters,
                  string endpointUrl,
                  long page = 1)
        {
            if (page <= 0) page = 1;

            var client = await GetAuthenticatedHttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            long offset = (page - 1) * StringExtensions.PageSize;

            queryString["limit"] = StringExtensions.PageSize.ToString();
            queryString["offset"] = offset.ToString();
            queryString.Add("metadata", "offset,count,limit");

            // Add the filters to the query string
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryString[$"q{filter.Key}"] = $"{filter.Value}";
                }
            }

            // Make the request
            var response = await client.GetAsync($"{endpointUrl}?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                return await response.FromJson<SimplicateDataRequest<T>>();
            }

            throw new Exception(response.ReasonPhrase);
        }

        private async Task<string> FetchSimplicateHtmlData<T>(
                      Dictionary<string, string> filters,
                      string endpointUrl,
                      long page = 1)
        {
            if (page <= 0) page = 1;
            var result = await FetchSimplicateData<T>(filters, endpointUrl, page);
            return result.Data.ToHtmlTable(page, result.Metadata.CalculateTotalPages(), result.Metadata.Count);

        }

    }

}


public class SimplicateDataRequest<T>
{
    public IEnumerable<T> Data { get; set; }

    public Metadata? Metadata { get; set; }


    public IEnumerable<string>? Errors { get; set; }
}



public static class SimplicateExtensions
{
    public static string Offset = "offset";
    public static string Count = "count";
    public static string Limit = "limit";
    private const string Metadata = "metadata";

    public static async Task<IEnumerable<T>> PagedRequest<T>(this HttpClient client, string url, int delayMilliseconds = 500)
    {
        List<T> items = new List<T>();
        var uri = new Uri(client.BaseAddress + url);
        uri = uri.AddParameter(Metadata, $"{Offset},{Count},{Limit}");

        int offset = 0;

        SimplicateDataRequest<T>? result = null;

        do
        {
            uri = uri.AddParameter(Offset, offset.ToString());

            var stopwatch = Stopwatch.StartNew();
            result = await client.SimplicateGetRequest<SimplicateDataRequest<T>>(uri);

            if (result != null)
            {
                items.AddRange(result.Data);

                offset = result.Metadata!.Offset + result.Metadata.Limit;
            }

            int timeOut = CalculateTimeout(delayMilliseconds, stopwatch.ElapsedMilliseconds);

            await Task.Delay(timeOut);
        }
        while (result?.Metadata!.Count > offset);

        return items;
    }


    public static async Task<T?> SimplicateGetRequest<T>(this HttpClient client, Uri uri)
    {
        //   uri = uri.AddParameter(Metadata, $"{Offset},{Count},{Limit}");
        return await client.SimplicateRequest<T>(uri, HttpMethod.Get);
    }


    private static int CalculateTimeout(int delayMilliseconds, long elapsedMilliseconds)
    {
        return Math.Max(delayMilliseconds - (int)elapsedMilliseconds, 0);
    }

    public static Uri AddParameter(this Uri url, string paramName, string paramValue)
    {
        if (string.IsNullOrEmpty(paramName)) throw new ArgumentNullException(nameof(paramName));
        if (string.IsNullOrEmpty(paramValue)) throw new ArgumentNullException(nameof(paramValue));

        var uriBuilder = new UriBuilder(url);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

        query[paramName] = paramValue;
        uriBuilder.Query = query.ToString();

        return uriBuilder.Uri;
    }

    private static async Task<T?> SimplicateRequest<T>(this HttpClient client, Uri uri, HttpMethod method, object? bodyContent = null)
    {
        using (var httpRequestMessage = new HttpRequestMessage
        {
            Method = method,
            RequestUri = uri,
            Content = bodyContent != null ? new StringContent(JsonConvert.SerializeObject(bodyContent), Encoding.UTF8, "application/json") : null
        })
        {

            using (var result = await client.SendAsync(httpRequestMessage))
            {
                return await result.HandleSimplicateResponse<T>();
            }
        }
    }

    private const string AuthenticationKeyHeader = "Authentication-Key";
    private const string AuthenticationSecretHeader = "Authentication-Secret";

    private static void SetAuthenticationHeaders(HttpRequestMessage httpRequestMessage, string key, string secret)
    {
        httpRequestMessage.Headers.Add(AuthenticationKeyHeader, key);
        httpRequestMessage.Headers.Add(AuthenticationSecretHeader, secret);
    }

    /// <summary>
    /// Handles the Simplicate API response and returns the deserialized object of type T or throws an appropriate exception.
    /// </summary>
    /// <typeparam name="T">The type of the response object.</typeparam>
    /// <param name="message">The HttpResponseMessage to be handled.</param>
    /// <returns>A task representing the response object of type T, or null if the response content is null.</returns>
    private static async Task<T?> HandleSimplicateResponse<T>(this HttpResponseMessage message)
    {
        switch (message.StatusCode)
        {
            case HttpStatusCode.OK:
                return await message.Content.ReadFromJsonAsync<T>();
            case HttpStatusCode.BadRequest:
                var errors = await message.Content.ReadFromJsonAsync<SimplicateErrorResponse>();

                if (errors != null && errors.Errors != null)
                {
                    throw new SimplicateResponseException((int)message.StatusCode, string.Join(',', errors.Errors.Select(y => y.Message)));
                }
                break;
            case System.Net.HttpStatusCode.NotFound:
            case System.Net.HttpStatusCode.Unauthorized:
            default:
                break;
        }

        throw new SimplicateResponseException((int)message.StatusCode);
    }
}



public class Metadata
{
    public int Count { get; set; }

    public int Offset { get; set; }

    public int Limit { get; set; }

}


public class SimplicateError
{
    public string Message { get; set; } = null!;
}
public class SimplicateErrorResponse
{
    public IEnumerable<SimplicateError>? Errors { get; set; }
}


public class SimplicateResponseException : Exception
{
    public SimplicateResponseException(int statusCode, object? value = null) =>
        (StatusCode, Value) = (statusCode, value);

    public int StatusCode { get; }

    public object? Value { get; }
}


