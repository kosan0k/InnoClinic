using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Services.Identity.Shared.Configurations;
using Services.Identity.Shared.Costants;
using Services.Shared.Authentication;
using Services.Shared.Configuration;
using StackExchange.Redis;

namespace Services.Identity.Features.Auth.Registration;

public static class IServiceCollectionExtensions
{
    private const string CookieName = "InnoClinic.Auth";
    private const string DataProtectionAppName = "InnoClinic";

    public static IServiceCollection ConfigureOpenIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IConnectionMultiplexer redisConnection,
        bool isDevelopment)
    {
        var authOptions = configuration.GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);
        var authority = $"{authOptions.KeycloakBaseUrl}/realms/{authOptions.Realm}";

        // Configure shared Data Protection using Redis (same keys as other services)
        services.AddDataProtection()
            .SetApplicationName(DataProtectionAppName)
            .PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys");

        // Configure authentication with JWT Bearer (default), Cookie, and OpenID Connect schemes
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
