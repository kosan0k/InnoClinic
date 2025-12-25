using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Api.Contracts;

public sealed record UpdateDoctorRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
    public required DoctorStatus Status { get; init; }
}

