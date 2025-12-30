using Services.Profiles.Application.Common.Events;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Events;

public sealed record DoctorStatusChangedEvent : SyncEventBase
{
    public override string EventType => nameof(DoctorStatusChangedEvent);
    
    public required Guid DoctorId { get; init; }
    public required DoctorStatus OldStatus { get; init; }
    public required DoctorStatus NewStatus { get; init; }
}

