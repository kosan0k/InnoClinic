using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.Shared.Configurations;
using Services.Identity.Shared.Costants;
using Services.Shared.Configuration;
using System.Security.Claims;
using System.Text.Json;

namespace Services.Identity.Features.Auth.Registration;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection ConfigureOpenIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var authOptions = configuration.GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);

        // Cookie authentication for session management
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => 
            {
                options.Authority = $"{authOptions.KeycloakBaseUrl}/realms/{authOptions.Realm}";

                options.RequireHttpsMetadata = !isDevelopment;

                // Keycloak access tokens often don't include the "aud" claim for the API itself by default.
                // For development, we disable audience validation or set it to "account".
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !isDevelopment, // Or set ValidAudience = "account"
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                    ValidateIssuer = true
                };

                //CRITICAL: Map Keycloak "realm_access" roles to .NET Claims
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity) 
                            return Task.CompletedTask;

                        // Parse the "realm_access" JSON property from the token
                        var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
                        if (realmAccessClaim != null)
                        {
                            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                            {
                                foreach (var role in rolesElement.EnumerateArray())
                                {
                                    // Add the role as a standard .NET Claim
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "InnoClinic.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, _ => { });

        // Configure OpenID Connect using IConfigureOptions to pick up Aspire environment variables
        services
            .AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                var authority = authOptions.Authority;
                var clientId = authOptions.ClientId;
                var clientSecret = authOptions.ClientSecret;

                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = !isDevelopment;

                // Disable Pushed Authorization Request (PAR) - Keycloak may not support it properly
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.CallbackPath = "/api/auth/oidc-callback";
                options.SignedOutCallbackPath = "/api/auth/signout-callback";

                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "roles";
            });

        return services;
    }
}
