namespace Services.Profiles.Api.Contracts;

public sealed record CreateDoctorRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string Email { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
}

