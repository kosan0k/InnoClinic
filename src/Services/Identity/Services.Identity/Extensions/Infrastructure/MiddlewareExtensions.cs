using Microsoft.AspNetCore.Builder;
using Services.Identity.Middleware;

namespace Services.Identity.Extensions.Infrastructure;

/// <summary>
/// Extension methods for registering middleware components.
/// </summary>
public static class MiddlewareExtensions
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



