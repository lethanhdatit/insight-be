using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


internal class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;

    public HttpClientService(HttpClient httpClient, IOptions<AppSettings> configuration)
    {
        _httpClient = httpClient;
        var requestTimeout = configuration.Value.SystemHttpRequestTimeout;
        _httpClient.Timeout = requestTimeout > 0 ? TimeSpan.FromSeconds(requestTimeout) : _httpClient.Timeout;
    }

    public async Task<T> GetAsync<T>(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint, headers);
        return await SendRequestAsync<T>(request, options);
    }

    public async Task<T> PostAsync<T>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint, headers, data);
        return await SendRequestAsync<T>(request, options);
    }

    public async Task<T> PutAsync<T>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Put, endpoint, headers, data);
        return await SendRequestAsync<T>(request, options);
    }

    public async Task DeleteAsync(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Delete, endpoint, headers);
        await SendRequestAsync<object>(request, options);
    }

    public async Task<(SucessT, FailedT, HttpStatusCode)> GetAsync<SucessT, FailedT>(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint, headers);
        return await SendRequestAsync<SucessT, FailedT>(request, options);
    }

    public async Task<(SucessT, FailedT, HttpStatusCode)> PostAsync<SucessT, FailedT>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint, headers, data);
        return await SendRequestAsync<SucessT, FailedT>(request, options);
    }

    public async Task<(SucessT, FailedT, HttpStatusCode)> PutAsync<SucessT, FailedT>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null)
    {
        using var request = CreateRequest(HttpMethod.Put, endpoint, headers, data);
        return await SendRequestAsync<SucessT, FailedT>(request, options);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, IDictionary<string, string> headers, dynamic data = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        AddHeaders(request, headers);
        if (data != null)
        {
            Type typeData = data.GetType();

            if (typeData == typeof(List<KeyValuePair<string, string>>))
            {
                request.Content = new FormUrlEncodedContent(data);
            }
            else if (typeData == typeof(MultipartFormDataContent))
            {
                request.Content = data;
            } 
            else if (typeData == typeof(FormUrlEncodedContent))
            {
                request.Content = data;
            }
            else
            {
                request.Content = JsonContent.Create(data);
            }

        }
        return request;
    }

    private async Task<T> SendRequestAsync<T>(HttpRequestMessage request, JsonSerializerOptions options = null)
    {
        var response = await _httpClient.SendAsync(request);

        if(!response.IsSuccessStatusCode)
        {
            var failedResponse = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(failedResponse);
        }

        var responseData = await response.Content.ReadAsStringAsync();

        var defaultOptions = options ?? new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        foreach (var item in SystemSerialization.JsonConverters)
            defaultOptions.Converters.Add(item);

        return JsonSerializer.Deserialize<T>(responseData, defaultOptions);
    }

    private async Task<(SucessT, FailedT, HttpStatusCode)> SendRequestAsync<SucessT, FailedT>(HttpRequestMessage request, JsonSerializerOptions options = null)
    {
        var defaultOptions = options ?? new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        foreach (var item in SystemSerialization.JsonConverters)
            defaultOptions.Converters.Add(item);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var failedResponseData = await response.Content.ReadAsStringAsync();
            return (default(SucessT), JsonSerializer.Deserialize<FailedT>(failedResponseData, defaultOptions), response.StatusCode);
        }

        var responseData = await response.Content.ReadAsStringAsync();
        return (JsonSerializer.Deserialize<SucessT>(responseData, defaultOptions), default(FailedT), response.StatusCode);
    }

    private static void AddHeaders(HttpRequestMessage request, IDictionary<string, string> headers)
    {
        if (headers == null) return;
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}