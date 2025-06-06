using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

public interface IHttpClientService
{
    Task<T> GetAsync<T>(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task<T> PostAsync<T>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task<T> PutAsync<T>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task<(SucessT successResponse, FailedT failedResponse, HttpStatusCode statusCode)> GetAsync<SucessT, FailedT>(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task<(SucessT successResponse, FailedT failedResponse, HttpStatusCode statusCode)> PostAsync<SucessT, FailedT>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task<(SucessT successResponse, FailedT failedResponse, HttpStatusCode statusCode)> PutAsync<SucessT, FailedT>(string endpoint, dynamic data, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);

    Task DeleteAsync(string endpoint, IDictionary<string, string> headers = null, JsonSerializerOptions options = null, TimeSpan? timeout = null);
}
