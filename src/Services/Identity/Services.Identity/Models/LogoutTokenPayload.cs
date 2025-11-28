using System.Text.Json.Serialization;

namespace Services.Identity.Models;

/// <summary>
/// Represents the claims in an OIDC Back-Channel Logout Token.
/// https://openid.net/specs/openid-connect-backchannel-1_0.html
/// </summary>
public record LogoutTokenPayload
{
    /// <summary>
    /// Issuer identifier (must match the expected issuer).
    /// </summary>
    [JsonPropertyName("iss")]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Subject identifier of the user being logged out.
    /// </summary>
    [JsonPropertyName("sub")]
    public string? Subject { get; init; }

    /// <summary>
    /// Audience (client ID this token is intended for).
    /// </summary>
    [JsonPropertyName("aud")]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Time at which the logout token was issued.
    /// </summary>
    [JsonPropertyName("iat")]
    public long IssuedAt { get; init; }

    /// <summary>
    /// JWT ID - unique identifier for this logout token.
    /// </summary>
    [JsonPropertyName("jti")]
    public string JwtId { get; init; } = string.Empty;

    /// <summary>
    /// Session ID to be logged out.
    /// Either sid or sub must be present.
    /// </summary>
    [JsonPropertyName("sid")]
    public string? SessionId { get; init; }

    /// <summary>
    /// Events claim indicating this is a logout token.
    /// Must contain "http://schemas.openid.net/event/backchannel-logout".
    /// </summary>
    [JsonPropertyName("events")]
    public Dictionary<string, object>? Events { get; init; }
}

/// <summary>
/// Request model for back-channel logout endpoint.
/// </summary>
public class BackChannelLogoutRequest
{
    /// <summary>
    /// The logout token JWT.
    /// </summary>
    public string LogoutToken { get; set; } = string.Empty;
}

