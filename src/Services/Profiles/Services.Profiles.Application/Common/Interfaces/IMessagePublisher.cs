namespace Services.Profiles.Application.Common.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(
        TMessage message,
        string routingKey,
        CancellationToken cancellationToken = default);
}
