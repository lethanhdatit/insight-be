using System;
using System.Threading.Tasks;

public class PainPublisher
{
    private readonly IQueueMessaging _mq;

    public PainPublisher(IQueueMessaging mq)
    {
        _mq = mq;
    }

    public async Task SubmitPainAsync(Guid id, string pain, string desire, string userAgent)
    {
        var message = new PainRequest
        {
            Pain = pain,
            Desire = desire,
            UserAgent = userAgent,
            PainId = id           
        };

        await _mq.PublishAsync(message, PainConsumer.QueueName);
    }
}
