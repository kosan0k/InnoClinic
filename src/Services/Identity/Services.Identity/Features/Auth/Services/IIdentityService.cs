using CSharpFunctionalExtensions;
using Services.Identity.Models;

namespace Services.Identity.Features.Auth.Services;

/// <summary>
/// Interface for identity management operations using Keycloak Admin API.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Registers a new user in Keycloak.
    /// </summary>
    /// <param name="request">The user registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the created user's Keycloak ID or an error.</returns>
    Task<Result<string>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a realm role to a user.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="roleName">The name of the realm role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a client role to a user.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="clientId">The Keycloak client ID (not client name).</param>
    /// <param name="roleName">The name of the client role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AssignClientRoleAsync(string userId, string clientId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their Keycloak ID.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the user or an error.</returns>
    Task<Result<KeycloakUser>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the user or an error.</returns>
    Task<Result<KeycloakUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from Keycloak.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}

