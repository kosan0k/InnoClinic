using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Shared.Configurations;
using Services.Identity.Shared.Costants;
using StackExchange.Redis;

namespace Services.Identity.Features.Session.Service;

/// <summary>
/// Redis-based implementation of session revocation.
/// Stores revoked session IDs with TTL matching access token lifespan.
/// </summary>
public class SessionRevocationService : ISessionRevocationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SessionRevocationService> _logger;
    private readonly RedisOptions _redisOptions;

    public SessionRevocationService(
        IConnectionMultiplexer redis,
        ILogger<SessionRevocationService> logger,
        IOptions<RedisOptions> redisOptions)
    {
        _redis = redis;
        _logger = logger;
        _redisOptions = redisOptions.Value;
    }

    private string GetRevocationKey(string sessionId) =>
        $"{_redisOptions.InstanceName}{AuthConstants.RedisKeys.RevokedSession}{sessionId}";

    public async Task<bool> RevokeSessionAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("Attempted to revoke null or empty session ID");
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetRevocationKey(sessionId);
            var value = DateTime.UtcNow.ToString("O"); // Store revocation timestamp

            var result = await db.StringSetAsync(key, value, ttl);

            if (result)
            {
                _logger.LogInformation(
                    "Revoked session {SessionId} with TTL {TtlMinutes} minutes",
                    sessionId,
                    ttl.TotalMinutes);
            }
            else
            {
                _logger.LogWarning("Failed to revoke session {SessionId} in Redis", sessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> IsSessionRevokedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetRevocationKey(sessionId);

            var exists = await db.KeyExistsAsync(key);

            if (exists)
            {
                _logger.LogDebug("Session {SessionId} is revoked", sessionId);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session revocation for {SessionId}", sessionId);
            
            // In case of Redis failure, we fail-open to avoid blocking all requests
            // This is a trade-off - adjust based on security requirements
            return false;
        }
    }

    public async Task<bool> RemoveRevocationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetRevocationKey(sessionId);

            var result = await db.KeyDeleteAsync(key);

            if (result)
            {
                _logger.LogInformation("Removed revocation for session {SessionId}", sessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing revocation for session {SessionId}", sessionId);
            return false;
        }
    }
}

