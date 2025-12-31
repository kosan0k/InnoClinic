using CSharpFunctionalExtensions;
using MediatR;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;

public sealed record GetDoctorProfileQuery : IRequest<Result<DoctorProfileVm, Exception>>
{
    public required Guid DoctorId { get; init; }
}

