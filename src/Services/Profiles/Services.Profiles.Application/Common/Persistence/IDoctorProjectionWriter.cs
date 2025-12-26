using Services.Profiles.Domain.Entities;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Common.Persistence;

/// <summary>
/// Responsible strictly for writing (projecting) changes to the Read Database.
/// This separates it from IDoctorReadRepository which is for Queries.
/// </summary>
public interface IDoctorProjectionWriter
{
    // Checks existence to support idempotency
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    // Create
    Task CreateAsync(Doctor doctor, CancellationToken cancellationToken);

    // Update (Full Replace)
    Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken);

    // Update (Partial - Status)
    Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken cancellationToken);
}
