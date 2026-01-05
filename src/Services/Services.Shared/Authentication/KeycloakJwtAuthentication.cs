using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Services.Shared.Authentication;

/// <summary>
/// Extension methods for configuring Keycloak authentication.
/// </summary>
public static class KeycloakJwtAuthentication
{
    private const string CookieName = "InnoClinic.Auth";
    private const string DataProtectionAppName = "InnoClinic";    

    /// <summary>
    /// Adds both Cookie and JWT Bearer authentication with shared Data Protection.
    /// Supports browser-based flows (Cookie with redirect) and API clients (JWT Bearer).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authority">The Keycloak realm URL.</param>
    /// <param name="identityLoginUrl">The Identity service login URL for redirects.</param>
    /// <param name="redisConnection">Redis connection for shared Data Protection keys.</param>
    /// <param name="isDevelopment">Whether the application is running in development mode.</param>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        string authority,
        string identityLoginUrl,
        IConnectionMultiplexer redisConnection,
        bool isDevelopment)
    {
        // Configure shared Data Protection using Redis
        services.AddDataProtection()
            .SetApplicationName(DataProtectionAppName)
            .PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys");

        // Configure authentication with multiple schemes
        // Default to JWT Bearer for API requests, but also accept cookies
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Smart";
                options.DefaultChallengeScheme = "Smart";
            })
            .AddPolicyScheme("Smart", "Smart Auth Selector", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // If Authorization header with Bearer token exists, use JWT
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }

                    // Otherwise, use Cookie authentication
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })
            .AddKeycloakJwtBearer(authority, isDevelopment)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // Redirect to Identity service login for unauthenticated requests
                options.LoginPath = PathString.Empty; // Disable default redirect
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        // For API requests (Accept: application/json), return 401
                        if (context.Request.Headers.Accept.ToString().Contains("application/json"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        // For browser requests, redirect to Identity login
                        var returnUrl = Uri.EscapeDataString(context.Request.GetDisplayUrl());
                        context.Response.Redirect($"{identityLoginUrl}?returnUrl={returnUrl}");
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adds Keycloak JWT Bearer scheme to an existing authentication builder.
    /// </summary>
    public static AuthenticationBuilder AddKeycloakJwtBearer(
        this AuthenticationBuilder builder,
        string authority,
        bool isDevelopment)
    {
        return builder.AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = !isDevelopment;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !isDevelopment,
                NameClaimType = "preferred_username",
                RoleClaimType = ClaimTypes.Role,
                ValidateIssuer = true
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    MapKeycloakRolesToClaims(context);
                    return Task.CompletedTask;
                }
            };
        });
    }

    /// <summary>
    /// Maps Keycloak realm_access roles to standard .NET role claims.
    /// </summary>
    private static void MapKeycloakRolesToClaims(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity)
            return;

        var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
        if (realmAccessClaim == null)
            return;

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrEmpty(roleName))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Invalid JSON in realm_access claim - skip role mapping
        }
    }
}

