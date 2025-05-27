using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class PainConsumer
{
    public const string QueueName = "pain.submit";

    public PainConsumer(IQueueMessaging mq, ILogger<PainConsumer> logger)
    {
        logger.LogInformation("PainConsumer initialized");

        mq.Subscribe<PainRequest>(QueueName, async (data) =>
        {
            logger.LogInformation("Pain request: {Id}", data.PainId);

            
            await Task.CompletedTask;
        });
    }
}
