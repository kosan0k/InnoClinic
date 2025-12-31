using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Application.Common.Persistence;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

public sealed class GetDoctorsListQueryHandler : IRequestHandler<GetDoctorsListQuery, Result<IReadOnlyList<DoctorListItemVm>, Exception>>
{
    private readonly IDoctorReadRepository _doctorReadRepository;

    public GetDoctorsListQueryHandler(IDoctorReadRepository doctorReadRepository)
    {
        _doctorReadRepository = doctorReadRepository;
    }

    public async Task<Result<IReadOnlyList<DoctorListItemVm>, Exception>> Handle(GetDoctorsListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var doctors = await _doctorReadRepository.GetAllAsync(cancellationToken);
            return Result.Success<IReadOnlyList<DoctorListItemVm>, Exception>(doctors);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<DoctorListItemVm>, Exception>(ex);
        }
    }
}
