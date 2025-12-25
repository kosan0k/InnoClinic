using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Common.Persistence;

public interface IDoctorReadRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
}

