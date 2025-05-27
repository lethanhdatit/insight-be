using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

public class PainConsumer
{
    public const string QueuePainLabeling = "pain.labeling";

    public PainConsumer(IQueueMessaging mq
        , ILogger<PainConsumer> logger
        , IServiceScopeFactory scopeFactory)
    {
        logger.LogInformation("PainConsumer initialized");

        mq.Subscribe<Guid>(QueuePainLabeling, async (data) =>
        {
            using var scope = scopeFactory.CreateScope();
            var painBusiness = scope.ServiceProvider.GetRequiredService<IPainBusiness>();

            await painBusiness.PainLabelingAsync(data);
        });
    }
}
