using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Commands.ChangeStatus;

public sealed record ChangeDoctorStatusCommand : IRequest<UnitResult<Exception>>
{
    public required Guid DoctorId { get; init; }
    public required DoctorStatus NewStatus { get; init; }
}

