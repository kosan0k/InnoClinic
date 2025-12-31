using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Common.Persistence;

/// <summary>
/// Repository for accessing Specialization data from the write database.
/// Used primarily for validation and syncing to read models.
/// </summary>
public interface ISpecializationRepository
{
    /// <summary>
    /// Gets a specialization by its unique identifier.
    /// </summary>
    Task<Specialization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specialization with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

