using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class RabbitMqService : IQueueMessaging, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly RetryPolicy _retryPolicy;
    private readonly QueueMessagingSettings _settings;

    public RabbitMqService(IOptions<QueueMessagingSettings> settings, ILogger<RabbitMqService> logger, RetryPolicy retryPolicy)
    {
        _logger = logger;
        _settings = settings.Value;

        _retryPolicy = retryPolicy;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync<T>(T message, string queueName)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);

        _logger.LogInformation("Published to {Queue}: {Data}", queueName, json);
        return Task.CompletedTask;
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize<T>(json);

                if (obj == null)
                    throw new InvalidDataException("Failed to deserialize message");

                await _retryPolicy.ExecuteAsync(() => handler(obj), messageId, retryCount: 0);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully handled message {MessageId} from {Queue}", messageId, queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId} from {Queue}", messageId, queueName);

                // Move to DLQ, or reject
                _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Subscribed to queue {Queue}", queueName);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

