using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

public class RetryPolicy
{
    private readonly QueueRetrySettings _settings;
    private readonly ILogger<RetryPolicy> _logger;
    private readonly Random _random = new();

    public RetryPolicy(IOptions<QueueMessagingSettings> settings, ILogger<RetryPolicy> logger)
    {
        _settings = settings.Value.Retry;
        _logger = logger;
    }

    public async Task ExecuteAsync(Func<Task> action, string messageId, int retryCount)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}. Retry #{RetryCount}", messageId, retryCount);

            if (retryCount >= _settings.MaxRetryCount)
            {
                _logger.LogWarning("Message {MessageId} exceeded max retries. Moving to DLQ.", messageId);
                throw;
            }

            var delay = _random.Next(_settings.MinDelayMs, _settings.MaxDelayMs);

            _logger.LogInformation("Retrying message {MessageId} in {Delay}ms", messageId, delay);

            await Task.Delay(delay);

            await ExecuteAsync(action, messageId, retryCount + 1);
        }
    }
}

