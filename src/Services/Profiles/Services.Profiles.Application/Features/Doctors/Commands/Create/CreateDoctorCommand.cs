using CSharpFunctionalExtensions;
using MediatR;

namespace Services.Profiles.Application.Features.Doctors.Commands.Create;

public sealed record CreateDoctorCommand : IRequest<Result<Guid, Exception>>
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string Email { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
    public required Guid SpecializationId { get; init; }
}
