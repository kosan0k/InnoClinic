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
        return await _doctorReadRepository.GetByIdAsync(request.DoctorId, cancellationToken);
    }
}
