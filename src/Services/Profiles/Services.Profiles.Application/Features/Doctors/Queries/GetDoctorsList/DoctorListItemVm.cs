using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

public sealed record DoctorListItemVm
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public string? PhotoUrl { get; init; }
    public required int Experience { get; init; }
    public required DoctorStatus Status { get; init; }
    public required string SpecializationName { get; init; }
    public required IReadOnlyList<string> Services { get; init; }
}
