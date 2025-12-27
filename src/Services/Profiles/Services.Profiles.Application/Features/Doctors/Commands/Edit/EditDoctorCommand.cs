using MediatR;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Commands.Edit;

public sealed record EditDoctorCommand : IRequest
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public required int CareerStartYear { get; init; }
    public required Guid SpecializationId { get; init; }
    public required DoctorStatus Status { get; init; }
}
