using MediatR;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Features.Doctors.Commands.ChangeStatus;

public sealed record ChangeDoctorStatusCommand : IRequest
{
    public required Guid DoctorId { get; init; }
    public required DoctorStatus NewStatus { get; init; }
}

