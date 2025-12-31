using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Api.Contracts;

public sealed record ChangeDoctorStatusRequest
{
    public required DoctorStatus Status { get; init; }
}

