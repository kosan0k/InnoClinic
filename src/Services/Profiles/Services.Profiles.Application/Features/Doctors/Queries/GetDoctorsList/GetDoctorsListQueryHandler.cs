using MediatR;
using Services.Profiles.Application.Common.Persistence;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

public sealed class GetDoctorsListQueryHandler : IRequestHandler<GetDoctorsListQuery, IReadOnlyList<DoctorListItemVm>>
{
    private readonly IDoctorReadRepository _doctorReadRepository;

    public GetDoctorsListQueryHandler(IDoctorReadRepository doctorReadRepository)
    {
        _doctorReadRepository = doctorReadRepository;
    }

    public async Task<IReadOnlyList<DoctorListItemVm>> Handle(GetDoctorsListQuery request, CancellationToken cancellationToken)
    {
        var doctors = await _doctorReadRepository.GetAllAsync(cancellationToken);
        var currentYear = DateTime.UtcNow.Year;

        return [.. doctors.Select(doctor => new DoctorListItemVm
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            MiddleName = doctor.MiddleName,
            PhotoUrl = doctor.PhotoUrl,
            Experience = currentYear - doctor.CareerStartYear,
            Status = doctor.Status
        })];
    }
}

