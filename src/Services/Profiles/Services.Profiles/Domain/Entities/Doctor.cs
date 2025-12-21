using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Domain.Entities;

public record Doctor
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public DateTime DateOfBirth { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? PhotoUrl { get; init; }

    public int CareerStartYear { get; init; }

    public DoctorStatus Status { get; init; } = DoctorStatus.AtWork;
}
