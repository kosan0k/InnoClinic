using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Application.Common.Persistence;

/// <summary>
/// Responsible strictly for writing (projecting) changes to the Read Database.
/// This separates it from IDoctorReadRepository which is for Queries.
/// </summary>
public interface IDoctorProjectionWriter
{
    /// <summary>
    /// Checks existence to support idempotency.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new doctor read model with flattened specialization data.
    /// </summary>
    Task CreateAsync(DoctorProjectionData data, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing doctor read model with flattened specialization data.
    /// </summary>
    Task UpdateAsync(DoctorProjectionData data, CancellationToken cancellationToken);

    /// <summary>
    /// Partial update for status changes only.
    /// </summary>
    Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the doctor projection from the read database (used for soft delete).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
