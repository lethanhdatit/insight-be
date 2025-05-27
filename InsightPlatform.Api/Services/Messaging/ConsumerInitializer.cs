using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ConsumerInitializer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerInitializer> _logger;

    public ConsumerInitializer(IServiceProvider serviceProvider, ILogger<ConsumerInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(3000, stoppingToken);

        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<PainConsumer>();

        _logger.LogInformation("Consumers initialized via hosted service");
    }
}

