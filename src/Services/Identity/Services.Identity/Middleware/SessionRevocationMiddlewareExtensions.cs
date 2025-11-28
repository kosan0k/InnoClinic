using Microsoft.AspNetCore.Builder;

namespace Services.Identity.Middleware;

/// <summary>
/// Extension methods for registering the session revocation middleware.
/// </summary>
public static class SessionRevocationMiddlewareExtensions
{
    /// <summary>
    /// Adds the session revocation middleware to the pipeline.
    /// Should be called after UseAuthentication().
    /// </summary>
    public static IApplicationBuilder UseSessionRevocation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionRevocationMiddleware>();
    }
}

