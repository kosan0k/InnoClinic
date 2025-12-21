using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Features.Doctors.Queries.GetDoctorProfile;

public record DoctorProfileVm
{
    public Guid Id { get; init; }
    public string? PhotoUrl { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public required string SpecializationName { get; init; }
    public IEnumerable<string> Services { get; init; } = []; 
    public required string OfficeAddress { get; init; }
    public int CareerStartYear { get; init; }
    public int Experience { get; init; }
    public DoctorStatus Status { get; init; }
}
