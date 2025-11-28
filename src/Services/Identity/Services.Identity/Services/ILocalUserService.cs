using CSharpFunctionalExtensions;
using Services.Identity.Entities;

namespace Services.Identity.Services;

/// <summary>
/// Interface for local user management operations.
/// </summary>
public interface ILocalUserService
{
    /// <summary>
    /// Creates a local user entity from Keycloak data.
    /// </summary>
    Task<Result<LocalUser>> CreateUserAsync(
        string keycloakUserId,
        string username,
        string email,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a local user by their Keycloak user ID.
    /// </summary>
    Task<Result<LocalUser>> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a local user by their email.
    /// </summary>
    Task<Result<LocalUser>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a local user by their Keycloak user ID.
    /// </summary>
    Task<Result> DeleteByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a local user's information.
    /// </summary>
    Task<Result<LocalUser>> UpdateUserAsync(
        string keycloakUserId,
        string? username = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default);
}

