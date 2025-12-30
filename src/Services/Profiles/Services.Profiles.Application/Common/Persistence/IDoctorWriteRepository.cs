using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Common.Persistence;

public interface IDoctorWriteRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);
    void Update(Doctor doctor);
    void Remove(Doctor doctor);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
