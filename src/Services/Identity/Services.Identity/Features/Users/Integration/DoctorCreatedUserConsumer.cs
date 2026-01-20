using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Services.Identity.Features.Users.Models;
using Services.Identity.Features.Users.Services;
using Services.Shared.Messaging.Messages.Doctors;
using Services.Shared.Messaging;

namespace Services.Identity.Features.Users.Integration;

public sealed class DoctorCreatedUserConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IIdentityService _identityService;
    private readonly ILogger<DoctorCreatedUserConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public DoctorCreatedUserConsumer(
        IConfiguration configuration,
        IIdentityService identityService,
        ILogger<DoctorCreatedUserConsumer> logger)
    {
        _configuration = configuration;
        _identityService = identityService;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("rabbitmq")
            ?? throw new InvalidOperationException("rabbitmq connection string is not configured");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: RabbitMqEventBus.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: RabbitMqEventBus.DoctorCreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: RabbitMqEventBus.DoctorCreatedQueue,
            exchange: RabbitMqEventBus.ExchangeName,
            routingKey: RabbitMqEventBus.DoctorCreatedRoutingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        _channel.BasicConsume(
            queue: RabbitMqEventBus.DoctorCreatedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("DoctorCreated consumer started.");

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var message = JsonSerializer.Deserialize<DoctorCreatedMessage>(args.Body.Span);
            if (message is null)
            {
                _logger.LogWarning("DoctorCreated message deserialization returned null.");
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var registerRequest = new RegisterUserRequest
            {
                Username = message.Email,
                Email = message.Email,
                Password = message.Email,
                FirstName = message.FirstName,
                LastName = message.LastName,
                Attributes = new Dictionary<string, List<string>>
                {
                    ["doctorId"] = [message.DoctorId.ToString()]
                }
            };

            var result = await _identityService.RegisterUserAsync(registerRequest, CancellationToken.None);

            if (result.IsFailure)
            {
                _logger.LogError(result.Error, "Failed to create user for doctor {DoctorId}", message.DoctorId);
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DoctorCreated message.");
            _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}

