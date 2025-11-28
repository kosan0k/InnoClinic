using System.Text.Json.Serialization;

namespace Services.Identity.Models;

/// <summary>
/// Payload structure for vymalo/keycloak-webhook plugin events.
/// </summary>
public record WebhookPayload
{
    /// <summary>
    /// The type of event (e.g., REGISTER, DELETE_ACCOUNT).
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// The Keycloak user ID associated with the event.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred (Unix timestamp in milliseconds).
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; init; }

    /// <summary>
    /// The realm where the event occurred.
    /// </summary>
    [JsonPropertyName("realmId")]
    public string RealmId { get; init; } = string.Empty;

    /// <summary>
    /// The realm name.
    /// </summary>
    [JsonPropertyName("realmName")]
    public string? RealmName { get; init; }

    /// <summary>
    /// The client ID that triggered the event.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }

    /// <summary>
    /// The session ID associated with the event.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }

    /// <summary>
    /// Additional event details.
    /// </summary>
    [JsonPropertyName("details")]
    public WebhookEventDetails? Details { get; init; }

    /// <summary>
    /// Any error message if the event represents an error.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Additional details for webhook events.
/// </summary>
public record WebhookEventDetails
{
    /// <summary>
    /// Username of the user.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>
    /// Email of the user.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// First name of the user.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name of the user.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    /// <summary>
    /// Authentication method used.
    /// </summary>
    [JsonPropertyName("auth_method")]
    public string? AuthMethod { get; init; }

    /// <summary>
    /// Authentication type.
    /// </summary>
    [JsonPropertyName("auth_type")]
    public string? AuthType { get; init; }

    /// <summary>
    /// Redirect URI used.
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; init; }

    /// <summary>
    /// Code ID for authorization code flow.
    /// </summary>
    [JsonPropertyName("code_id")]
    public string? CodeId { get; init; }

    /// <summary>
    /// Register method (e.g., admin, self-registration).
    /// </summary>
    [JsonPropertyName("register_method")]
    public string? RegisterMethod { get; init; }
}

/// <summary>
/// Known Keycloak webhook event types from vymalo plugin.
/// </summary>
public static class WebhookEventTypes
{
    public const string Register = "REGISTER";
    public const string DeleteAccount = "DELETE_ACCOUNT";
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string LoginError = "LOGIN_ERROR";
    public const string UpdateEmail = "UPDATE_EMAIL";
    public const string UpdateProfile = "UPDATE_PROFILE";
    public const string ResetPassword = "RESET_PASSWORD";
    public const string VerifyEmail = "VERIFY_EMAIL";
}

