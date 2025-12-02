namespace Services.Identity.Features.Session.Service;

/// <summary>
/// Interface for session revocation operations using Redis.
/// </summary>
public interface ISessionRevocationService
{
    /// <summary>
    /// Revokes a session by storing its ID in Redis.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="ttl">Time-to-live for the revocation entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was successfully revoked.</returns>
    Task<bool> RevokeSessionAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session has been revoked.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session is revoked.</returns>
    Task<bool> IsSessionRevokedAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a session revocation entry (e.g., for cleanup).
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entry was removed.</returns>
    Task<bool> RemoveRevocationAsync(string sessionId, CancellationToken cancellationToken = default);
}

