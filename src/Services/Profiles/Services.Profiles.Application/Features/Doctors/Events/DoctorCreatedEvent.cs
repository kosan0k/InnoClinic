using Services.Profiles.Application.Common.Events;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Events;

public sealed record DoctorCreatedEvent : SyncEventBase
{
    public override string EventType => nameof(DoctorCreatedEvent);
    
    public required Guid DoctorId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string Email { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
    public required Guid SpecializationId { get; init; }
    public required DoctorStatus Status { get; init; }
}
