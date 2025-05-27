using System.Threading.Tasks;
using System;

public interface IQueueMessaging
{
    Task PublishAsync<T> (T message, string queueName);

    void Subscribe<T> (string queueName, Func<T, Task> handler);
}
