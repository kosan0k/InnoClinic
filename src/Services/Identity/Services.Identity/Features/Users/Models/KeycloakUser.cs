namespace Services.Identity.Features.Users.Models;

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

