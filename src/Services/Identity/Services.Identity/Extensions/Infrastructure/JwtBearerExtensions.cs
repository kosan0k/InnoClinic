using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.Authentication;
using Services.Identity.Configurations;
using Services.Identity.Constants;

namespace Services.Identity.Extensions.Infrastructure;

/// <summary>
/// Extension methods for configuring JWT Bearer authentication with Keycloak.
/// </summary>
public static class JwtBearerExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication for the Auth.Service (Resource Server pattern).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authOptions">Authentication configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJwtBearerAuthentication(
        this IServiceCollection services,
        AuthOptions authOptions)
    {
        // Register the OpenID Connect configuration manager for key retrieval
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
        {
            var metadataAddress = $"{authOptions.Authority}/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        });

        // Register the claims transformer
        services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformer>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Discovery: Use the .well-known/openid-configuration endpoint
                options.Authority = authOptions.Authority;
                options.MetadataAddress = $"{authOptions.Authority}/.well-known/openid-configuration";
                
                // For development with HTTP Keycloak
                options.RequireHttpsMetadata = !authOptions.Authority.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

                // Strict Validation
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate the issuer
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Authority,

                    // Validate the audience
                    ValidateAudience = true,
                    ValidAudiences = authOptions.ValidAudiences.Length > 0 
                        ? authOptions.ValidAudiences 
                        : [authOptions.ClientId, "account"],

                    // Validate the token lifetime
                    ValidateLifetime = true,

                    // Validate the signing key
                    ValidateIssuerSigningKey = true,

                    // Clock skew tolerance (1 minute as specified)
                    ClockSkew = TimeSpan.FromSeconds(authOptions.ClockSkewSeconds),

                    // Use Keycloak's claim names
                    NameClaimType = AuthConstants.Keycloak.Claims.PreferredUsername,
                    RoleClaimType = ClaimTypes.Role
                };

                // Map claims properly (don't rename them)
                options.MapInboundClaims = false;

                // Tell the middleware to resolve the events from DI
                options.EventsType = typeof(KeycloakJwtBearerEvents);
            });

        // Add authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.Policies.AdminOnly, policy =>
                policy.RequireRole(AuthConstants.Roles.Admin))
            .AddPolicy(AuthConstants.Policies.UserManagement, policy =>
                policy.RequireRole(AuthConstants.Roles.Admin, "user-management"));

        return services;
    }
}

