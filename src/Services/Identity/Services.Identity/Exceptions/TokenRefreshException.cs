using Duende.IdentityModel.Client;
using System.Security.Authentication;

namespace Services.Identity.Exceptions;

/// <summary>
/// Thrown when the identity provider (e.g., Keycloak) fails to refresh a token.
/// This often indicates a revoked session or expired refresh token.
/// </summary>
public class TokenRefreshException : AuthenticationException
{
    public ResponseErrorType ErrorType { get; }
    public string? Error { get; }

    public TokenRefreshException(
        ResponseErrorType errorType,
        string? error,
        string? errorDescription)
        : base(errorDescription ?? error ?? "Token refresh failed. Session is likely revoked.")
    {
        ErrorType = errorType;
        Error = error;
    }

    public TokenRefreshException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorType = ResponseErrorType.Exception;
        Error = innerException.GetType().Name;        
    }
}
