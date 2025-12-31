using Services.Profiles.Application.Common.Events;

namespace Services.Profiles.Application.Features.Doctors.Events;

public sealed record DoctorDeletedEvent : SyncEventBase
{
    public override string EventType => nameof(DoctorDeletedEvent);

    public required Guid DoctorId { get; init; }
}

