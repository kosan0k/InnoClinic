namespace Services.Profiles.Application.Common.Events;

public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}

