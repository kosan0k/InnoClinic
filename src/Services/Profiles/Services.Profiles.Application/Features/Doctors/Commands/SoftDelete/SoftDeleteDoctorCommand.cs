using CSharpFunctionalExtensions;
using MediatR;

namespace Services.Profiles.Application.Features.Doctors.Commands.SoftDelete;

public sealed record SoftDeleteDoctorCommand : IRequest<UnitResult<Exception>>
{
    public required Guid DoctorId { get; init; }
}

