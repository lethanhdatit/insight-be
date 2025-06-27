using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IGeminiAIService
{
    Task<string> SendChatAsync(string systemPrompt);
}

public class GeminiAIService : IGeminiAIService
{
    private readonly IHttpClientService _httpClientService;
    private readonly AISettings _settings;
    private const int HttpClientTimeOut = 300;

    public GeminiAIService(IOptions<AISettings> settings, IHttpClientService httpClientService, IConfiguration config)
    {
        _httpClientService = httpClientService;
        _settings = settings.Value;
    }

    public async Task<string> SendChatAsync(string systemPrompt)
    {
        var request = new
        {
            contents = new List<dynamic>
            {
                new
                {
                    parts = new List<dynamic>
                    {
                        new
                        {
                            text = systemPrompt
                        }
                    }
                }
            }
        };

        var url = $"{_settings.BaseUrl.WithPath(_settings.Model)}:generateContent".WithQuery(new 
        {
            key = _settings.ApiKey,
        });

        var (passed, failed, statusCode) = await _httpClientService.PostAsync<GeminiAIApiResponse, GeminiAIApiFailedResponse>(url, 
            request,
            timeout: TimeSpan.FromSeconds(HttpClientTimeOut)
        );

        if (failed != null)
        {
            throw new BusinessException("GeminiAI_Error", failed.Error?.Message);
        }

        return passed.Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
    }
}
