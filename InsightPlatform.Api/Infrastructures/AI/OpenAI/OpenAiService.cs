using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IOpenAiService
{
    Task<string> SendChatAsync(string systemPrompt, string userPrompt);
}

public class OpenAiService : IOpenAiService
{
    private readonly IHttpClientService _httpClientService;
    private readonly OpenAISettings _settings;

    public OpenAiService(IOptions<OpenAISettings> settings, IHttpClientService httpClientService, IConfiguration config)
    {
        _httpClientService = httpClientService;
        _settings = settings.Value;
    }

    public async Task<string> SendChatAsync(string systemPrompt, string userPrompt)
    {
        var request = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var (passed, failed, statusCode) = await _httpClientService.PostAsync<ChatCompletionResponse, ChatCompletionFailedResponse>(_settings.BaseUrl, request, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {_settings.ApiKey}" }
        });

        if (failed != null)
        {
            if (statusCode == System.Net.HttpStatusCode.TooManyRequests &&
                (failed.Error.Type == "insufficient_quota"
                || failed.Error.Code == "insufficient_quota"))
                throw new BusinessException("OpenAI_OutOfBudget", failed.Error?.Message);

            throw new BusinessException("OpenAI_Error", failed.Error?.Message);
        }       

        if (passed?.Choices?.Any() == true)
        {
            foreach (var item in passed.Choices)
            {
                if (item.Message?.Content?.IsPresent() == true)
                {
                    item.Message.Content = item.Message.Content.Replace("```\n", string.Empty)
                                                               .Replace("\n```", string.Empty)
                                                               .Trim();
                }
            }
        }

        return passed.Choices.FirstOrDefault()?.Message.Content;
    }
}
