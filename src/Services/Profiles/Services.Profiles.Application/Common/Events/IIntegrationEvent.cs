using MediatR;

namespace Services.Profiles.Application.Common.Events;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

