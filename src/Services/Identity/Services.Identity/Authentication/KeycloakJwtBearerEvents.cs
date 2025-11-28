using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Services.Identity.Constants;

namespace Services.Identity.Authentication;

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
        // Adjust AuthConstants usage based on your actual namespace/class structure
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
}
