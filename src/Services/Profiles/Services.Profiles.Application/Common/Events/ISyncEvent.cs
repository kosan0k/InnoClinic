using MediatR;

namespace Services.Profiles.Application.Common.Events;

public interface ISyncEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

