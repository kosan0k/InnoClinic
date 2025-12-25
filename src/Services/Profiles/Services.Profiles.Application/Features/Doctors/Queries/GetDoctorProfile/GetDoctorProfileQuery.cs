using MediatR;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;

public sealed record GetDoctorProfileQuery : IRequest<DoctorProfileVm?>
{
    public required Guid DoctorId { get; init; }
}

