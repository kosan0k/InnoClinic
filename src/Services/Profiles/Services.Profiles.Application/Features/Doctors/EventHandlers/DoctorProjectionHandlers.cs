using MediatR;
using Microsoft.Extensions.Logging;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.EventHandlers;

public class DoctorProjectionHandlers :
    INotificationHandler<DoctorCreatedEvent>,
    INotificationHandler<DoctorUpdatedEvent>,
    INotificationHandler<DoctorStatusChangedEvent>
{
    private readonly IDoctorProjectionWriter _writer;

    public DoctorProjectionHandlers(IDoctorProjectionWriter writer)
    {
        _writer = writer;
    }

    public async Task Handle(DoctorCreatedEvent notification, CancellationToken ct)
    {
        if (await _writer.ExistsAsync(notification.DoctorId, ct))
        {
            return;
        }

        var doctor = new Doctor
        {
            Id = notification.DoctorId,
            FirstName = notification.FirstName,
            LastName = notification.LastName,
            MiddleName = notification.MiddleName,
            DateOfBirth = notification.DateOfBirth,
            Email = notification.Email,
            PhotoUrl = notification.PhotoUrl,
            CareerStartYear = notification.CareerStartYear,
            Status = notification.Status
        };

        await _writer.CreateAsync(doctor, ct);
    }

    public async Task Handle(DoctorUpdatedEvent notification, CancellationToken ct)
    {
        var doctor = new Doctor
        {
            Id = notification.DoctorId,
            FirstName = notification.FirstName,
            LastName = notification.LastName,
            MiddleName = notification.MiddleName,
            DateOfBirth = notification.DateOfBirth,
            Email = notification.Email,
            PhotoUrl = notification.PhotoUrl,
            CareerStartYear = notification.CareerStartYear,
            Status = notification.Status
        };

        await _writer.UpdateAsync(doctor, ct);
    }

    public async Task Handle(DoctorStatusChangedEvent notification, CancellationToken ct)
    {
        await _writer.UpdateStatusAsync(notification.DoctorId, notification.NewStatus, ct);
    }
}
