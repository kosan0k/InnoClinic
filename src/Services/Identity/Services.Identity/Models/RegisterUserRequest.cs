using System.ComponentModel.DataAnnotations;

namespace Services.Identity.Models;

/// <summary>
/// Request model for user registration.
/// </summary>
public record RegisterUserRequest
{
    /// <summary>
    /// The username for the new user.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Username { get; init; }

    /// <summary>
    /// The email address for the new user.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// The password for the new user.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    /// <summary>
    /// The user's first name.
    /// </summary>
    [StringLength(100)]
    public string? FirstName { get; init; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    [StringLength(100)]
    public string? LastName { get; init; }

    /// <summary>
    /// Whether the user account should be enabled immediately.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Whether the email should be marked as verified.
    /// </summary>
    public bool EmailVerified { get; init; } = false;

    /// <summary>
    /// Optional attributes to set on the user.
    /// </summary>
    public Dictionary<string, List<string>>? Attributes { get; init; }
}

/// <summary>
/// Keycloak user model.
/// </summary>
public record KeycloakUser
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool Enabled { get; init; }
    public bool EmailVerified { get; init; }
    public long CreatedTimestamp { get; init; }
    public Dictionary<string, List<string>>? Attributes { get; init; }
}

