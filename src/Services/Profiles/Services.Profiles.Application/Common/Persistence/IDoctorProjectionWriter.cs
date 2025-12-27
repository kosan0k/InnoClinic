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
    Task CreateAsync(
        Guid id,
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string email,
        string? photoUrl,
        int careerStartYear,
        DoctorStatus status,
        Guid specializationId,
        string specializationName,
        List<string> services,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing doctor read model with flattened specialization data.
    /// </summary>
    Task UpdateAsync(
        Guid id,
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string email,
        string? photoUrl,
        int careerStartYear,
        DoctorStatus status,
        Guid specializationId,
        string specializationName,
        List<string> services,
        CancellationToken cancellationToken);

    /// <summary>
    /// Partial update for status changes only.
    /// </summary>
    Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken cancellationToken);
}
