using MediatR;
using Services.Profiles.Application.Common.Persistence;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;

public sealed class GetDoctorProfileQueryHandler : IRequestHandler<GetDoctorProfileQuery, DoctorProfileVm?>
{
    private readonly IDoctorReadRepository _doctorReadRepository;

    public GetDoctorProfileQueryHandler(IDoctorReadRepository doctorReadRepository)
    {
        _doctorReadRepository = doctorReadRepository;
    }

    public async Task<DoctorProfileVm?> Handle(GetDoctorProfileQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorReadRepository.GetByIdAsync(request.DoctorId, cancellationToken);

        if (doctor is null)
        {
            return null;
        }

        return new DoctorProfileVm
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            MiddleName = doctor.MiddleName,
            DateOfBirth = doctor.DateOfBirth,
            Email = doctor.Email,
            PhotoUrl = doctor.PhotoUrl,
            CareerStartYear = doctor.CareerStartYear,
            Experience = DateTime.UtcNow.Year - doctor.CareerStartYear,
            Status = doctor.Status
        };
    }
}

