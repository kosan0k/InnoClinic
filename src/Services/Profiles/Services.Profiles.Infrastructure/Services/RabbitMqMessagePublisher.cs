using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Services.Profiles.Application.Common.Interfaces;
using Services.Shared.Messaging;

namespace Services.Profiles.Infrastructure.Services;

public sealed class RabbitMqMessagePublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(IConnection connection, ILogger<RabbitMqMessagePublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: RabbitMqEventBus.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: RabbitMqEventBus.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: payload);

        _logger.LogInformation("Published message {RoutingKey}", routingKey);
        return Task.CompletedTask;
    }
}
