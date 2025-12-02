namespace Services.Identity.Shared.Configurations;

/// <summary>
/// Configuration options for Keycloak authentication and authorization.
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// The Keycloak realm URL (e.g., http://keycloak:8080/realms/AppRealm)
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    
    /// <summary>
    /// The client ID for this service (e.g., auth-service-api)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// The client secret for this service
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// The admin client ID for Keycloak Admin API access (e.g., admin-cli)
    /// </summary>
    public string AdminClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// The admin client secret for Keycloak Admin API access
    /// </summary>
    public string AdminClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// The Keycloak realm name (e.g., AppRealm)
    /// </summary>
    public string Realm { get; set; } = string.Empty;
    
    /// <summary>
    /// The Keycloak base URL (e.g., http://keycloak:8080)
    /// </summary>
    public string KeycloakBaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Valid audiences for JWT validation
    /// </summary>
    public string[] ValidAudiences { get; set; } = [];
    
    /// <summary>
    /// Clock skew tolerance for token validation (in seconds)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 60;

#if DEBUG
    /// <summary>
    /// Development certificate path for HTTPS in local development
    /// </summary>
    public string DevCertPath { get; set; } = string.Empty;
#endif
}
