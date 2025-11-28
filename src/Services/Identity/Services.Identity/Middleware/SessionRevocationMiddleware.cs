using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Identity.Constants;
using Services.Identity.Services;

namespace Services.Identity.Middleware;

/// <summary>
/// Middleware that checks if the current request's session has been revoked.
/// Should run after authentication middleware.
/// </summary>
public class SessionRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionRevocationMiddleware> _logger;

    public SessionRevocationMiddleware(
        RequestDelegate next,
        ILogger<SessionRevocationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionRevocationService sessionRevocationService)
    {
        // Skip revocation check for unauthenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Extract session ID from the JWT token
        var sessionId = ExtractSessionId(context);

        if (string.IsNullOrEmpty(sessionId))
        {
            // No session ID in token - allow the request to proceed
            // This might happen with tokens that don't include 'sid' claim
            _logger.LogDebug("No session ID found in token, skipping revocation check");
            await _next(context);
            return;
        }

        // Check if the session has been revoked
        var isRevoked = await sessionRevocationService.IsSessionRevokedAsync(sessionId, context.RequestAborted);

        if (isRevoked)
        {
            _logger.LogWarning(
                "Request denied: Session {SessionId} has been revoked for user {UserId}",
                sessionId,
                context.User.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value ?? "unknown");

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = $"{AuthConstants.Schemes.Bearer} error=\"invalid_token\", error_description=\"Session has been revoked\"";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "session_revoked",
                error_description = "Your session has been terminated. Please log in again."
            });
            
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Extracts the session ID (sid) claim from the Bearer token.
    /// </summary>
    private string? ExtractSessionId(HttpContext context)
    {
        // Try to get from claims first (if already parsed)
        var sidClaim = context.User.FindFirst(AuthConstants.Keycloak.Claims.SessionId);
        if (sidClaim != null)
        {
            return sidClaim.Value;
        }

        // Fallback: Extract from Authorization header and parse the JWT
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == AuthConstants.Keycloak.Claims.SessionId)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract session ID from token");
            return null;
        }
    }
}