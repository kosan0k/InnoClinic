using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;
using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

namespace Services.Profiles.Application.Common.Persistence;

public interface IDoctorReadRepository
{
    Task<DoctorProfileVm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorListItemVm>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorListItemVm>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
}
