using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Common.Persistence;

/// <summary>
/// Repository for accessing Service data from the write database.
/// Provides extensibility for managing the service catalog.
/// </summary>
public interface IServiceRepository
{
    /// <summary>
    /// Gets a service by its unique identifier.
    /// </summary>
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active services.
    /// </summary>
    Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a service with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

