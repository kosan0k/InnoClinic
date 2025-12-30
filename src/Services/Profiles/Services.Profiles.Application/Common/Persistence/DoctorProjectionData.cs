using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Common.Persistence;

/// <summary>
/// Represents data required for creating or updating a doctor projection in the read database.
/// </summary>
public sealed record DoctorProjectionData
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string Email { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
    public required DoctorStatus Status { get; init; }
    public required Guid SpecializationId { get; init; }
    public required string SpecializationName { get; init; }
    public required List<string> Services { get; init; }
}

