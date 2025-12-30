namespace Services.Profiles.Application.Common.Events;

public abstract record SyncEventBase : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required DateTime OccurredOn { get; init; }
    public abstract string EventType { get; }
}
