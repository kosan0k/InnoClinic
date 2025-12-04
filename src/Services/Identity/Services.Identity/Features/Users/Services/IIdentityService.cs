using CSharpFunctionalExtensions;
using Services.Identity.Features.Users.Models;

namespace Services.Identity.Features.Users.Services;

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
    Task<Result<string, Exception>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}

