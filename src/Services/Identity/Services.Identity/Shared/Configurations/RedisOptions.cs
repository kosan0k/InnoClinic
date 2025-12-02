namespace Services.Identity.Shared.Configurations;

/// <summary>
/// Configuration options for Redis connection and session management.
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Redis connection string (e.g., redis:6379,password=secret)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Instance name prefix for Redis keys
    /// </summary>
    public string InstanceName { get; set; } = "AuthService_";
    
    /// <summary>
    /// TTL for revoked session IDs in minutes (should match access token lifespan)
    /// </summary>
    public int SessionRevocationTtlMinutes { get; set; } = 15;
}

