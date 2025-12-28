using MediatR;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;

namespace Services.Profiles.Application.Features.Doctors.EventHandlers;

public class DoctorProjectionHandlers :
    INotificationHandler<DoctorCreatedEvent>,
    INotificationHandler<DoctorUpdatedEvent>,
    INotificationHandler<DoctorStatusChangedEvent>,
    INotificationHandler<DoctorDeletedEvent>
{
    private readonly IDoctorProjectionWriter _writer;
    private readonly ISpecializationRepository _specializationRepository;

    public DoctorProjectionHandlers(
        IDoctorProjectionWriter writer,
        ISpecializationRepository specializationRepository)
    {
        _writer = writer;
        _specializationRepository = specializationRepository;
    }

    public async Task Handle(DoctorCreatedEvent notification, CancellationToken ct)
    {
        if (await _writer.ExistsAsync(notification.DoctorId, ct))
        {
            return;
        }

        // Fetch specialization details (with services) from write database
        var specialization = await _specializationRepository.GetByIdAsync(notification.SpecializationId, ct) 
            ?? throw new InvalidOperationException($"Specialization with ID {notification.SpecializationId} not found during projection sync.");

        // Extract service names from the Service entities
        var serviceNames = specialization.Services
            .Select(s => s.Name)
            .ToList();

        await _writer.CreateAsync(
            id: notification.DoctorId,
            firstName: notification.FirstName,
            lastName: notification.LastName,
            middleName: notification.MiddleName,
            dateOfBirth: notification.DateOfBirth,
            email: notification.Email,
            photoUrl: notification.PhotoUrl,
            careerStartYear: notification.CareerStartYear,
            status: notification.Status,
            specializationId: notification.SpecializationId,
            specializationName: specialization.Name,
            services: serviceNames,
            cancellationToken: ct);
    }

    public async Task Handle(DoctorUpdatedEvent notification, CancellationToken ct)
    {
        // Fetch specialization details (with services) from write database
        var specialization = await _specializationRepository.GetByIdAsync(notification.SpecializationId, ct) 
            ?? throw new InvalidOperationException($"Specialization with ID {notification.SpecializationId} not found during projection sync.");

        // Extract service names from the Service entities
        var serviceNames = specialization.Services
            .Select(s => s.Name)
            .ToList();

        await _writer.UpdateAsync(
            id: notification.DoctorId,
            firstName: notification.FirstName,
            lastName: notification.LastName,
            middleName: notification.MiddleName,
            dateOfBirth: notification.DateOfBirth,
            email: notification.Email,
            photoUrl: notification.PhotoUrl,
            careerStartYear: notification.CareerStartYear,
            status: notification.Status,
            specializationId: notification.SpecializationId,
            specializationName: specialization.Name,
            services: serviceNames,
            cancellationToken: ct);
    }

    public async Task Handle(DoctorStatusChangedEvent notification, CancellationToken ct)
    {
        await _writer.UpdateStatusAsync(notification.DoctorId, notification.NewStatus, ct);
    }

    public async Task Handle(DoctorDeletedEvent notification, CancellationToken ct)
    {
        await _writer.DeleteAsync(notification.DoctorId, ct);
    }
}
