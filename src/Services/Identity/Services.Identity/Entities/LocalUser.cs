namespace Services.Identity.Entities;

/// <summary>
/// Local user entity that mirrors Keycloak user data.
/// Used for application-specific user data and relationships.
/// </summary>
public class LocalUser
{
    /// <summary>
    /// Internal database ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Keycloak user ID (external identity).
    /// </summary>
    public string KeycloakUserId { get; set; } = string.Empty;

    /// <summary>
    /// Username from Keycloak.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address from Keycloak.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the user was created locally.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the user was synced from Keycloak.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Additional metadata stored as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

