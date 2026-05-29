using MediatR;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Shared.Messaging.Messages.Doctors;
using Services.Shared.Messaging;

namespace Services.Profiles.Application.Features.Doctors.EventHandlers;

public sealed class DoctorMessageHandler : INotificationHandler<DoctorCreatedEvent>
{
    private readonly IMessagePublisher _publisher;

    public DoctorMessageHandler(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(DoctorCreatedEvent notification, CancellationToken cancellationToken)
    {
        var message = new DoctorCreatedMessage
        {
            EventId = notification.EventId,
            OccurredOn = notification.OccurredOn,
            DoctorId = notification.DoctorId,
            Email = notification.Email,
            FirstName = notification.FirstName,
            LastName = notification.LastName,
            MiddleName = notification.MiddleName
        };

        return _publisher.PublishAsync(
            message,
            RabbitMqEventBus.DoctorCreatedRoutingKey,
            cancellationToken);
    }
}
