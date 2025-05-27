using System;
using System.Threading.Tasks;

public class PainPublisher
{
    private readonly IQueueMessaging _mq;

    public PainPublisher(IQueueMessaging mq)
    {
        _mq = mq;
    }

    public async Task PainLabelingAsync(Guid painId)
    {
        await _mq.PublishAsync(painId, PainConsumer.QueuePainLabeling);
    }
}
