using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Services.Identity.Constants;

namespace Services.Identity.Authentication;

/// <summary>
/// Custom JWT Bearer events for Keycloak authentication logging and debugging.
/// </summary>
public class KeycloakJwtBearerEvents : JwtBearerEvents
{
    private readonly ILogger<KeycloakJwtBearerEvents> _logger;

    public KeycloakJwtBearerEvents(ILogger<KeycloakJwtBearerEvents> logger)
    {
        _logger = logger;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogWarning(
            context.Exception,
            "JWT authentication failed: {Message}",
            context.Exception.Message);

        return base.AuthenticationFailed(context);
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value;
        _logger.LogDebug("Token validated for user {UserId}", userId);

        return base.TokenValidated(context);
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        _logger.LogDebug(
            "JWT challenge issued: {Error} - {ErrorDescription}",
            context.Error,
            context.ErrorDescription);

        return base.Challenge(context);
    }

    public override Task MessageReceived(MessageReceivedContext context)
    {
        _logger.LogTrace("JWT message received");
        return base.MessageReceived(context);
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        _logger.LogWarning("JWT forbidden - insufficient permissions");
        return base.Forbidden(context);
    }
}
