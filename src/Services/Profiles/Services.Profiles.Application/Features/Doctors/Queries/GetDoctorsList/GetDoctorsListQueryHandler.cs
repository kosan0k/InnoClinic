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
        return await _doctorReadRepository.GetAllAsync(cancellationToken);
    }
}
