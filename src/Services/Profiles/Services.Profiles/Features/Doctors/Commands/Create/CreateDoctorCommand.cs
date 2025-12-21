using MediatR;

namespace Services.Profiles.Features.Doctors.Commands.Create;

public class CreateDoctorCommand : IRequest<Guid>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public int CareerStartYear { get; init; }
}
