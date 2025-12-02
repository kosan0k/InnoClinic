using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Services.Identity.Configurations;
using Services.Identity.Constants;
using Services.Identity.Data;
using Services.Identity.Extensions.Infrastructure;
using Services.Shared.Configuration;

namespace Services.Identity.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        #region Aspire Service Defaults

        builder.AddServiceDefaults();

        #endregion

        #region Aspire Resource Connections

        // Add PostgreSQL using Aspire's connection handling
        builder.AddNpgsqlDbContext<AuthDbContext>("identitydb", configureDbContextOptions: options =>
        {
            options.UseNpgsql(npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Add Redis using Aspire's connection handling
        builder.AddRedisClient("redis");

        #endregion

        #region Configuration Binding

        var authOptions = builder.Configuration
            .GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);

        var redisOptions = builder.Configuration
            .GetOptions<RedisOptions>(AuthConstants.ConfigSections.RedisOptions);        

        #endregion

        #region Keycloak Authentication

        var isDevelopment = builder.Environment.IsDevelopment();

        // JWT Bearer for API authentication
        builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration, options =>
        {
            options.RequireHttpsMetadata = !isDevelopment;
        });

        // Cookie authentication for session management
        builder.Services.AddAuthentication()
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
        builder.Services
            .AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var authOptions = configuration.GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);

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

        #endregion

        #region Core Services

        // Add Auth services (session management, identity service)
        builder.Services.AddAuthServicesWithAspire();

        // Add controllers
        builder.Services.AddControllers();

        // Add OpenAPI with Scalar UI
        builder.AddDefaultOpenApi(
            title: "InnoClinic Identity API",
            version: "v1",
            description: "Authentication and identity management service for InnoClinic platform");

        #endregion

        var app = builder.Build();

        #region Database Migration

        if (app.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            try
            {
                await dbContext.Database.MigrateAsync();
                app.Logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Database migration failed - database may not be ready yet");
                try
                {
                    await dbContext.Database.EnsureCreatedAsync();
                    app.Logger.LogInformation("Database created successfully");
                }
                catch (Exception createEx)
                {
                    app.Logger.LogError(createEx, "Failed to create database");
                }
            }
        }

        #endregion

        #region Middleware Pipeline

        // Map OpenAPI and Scalar UI (development only by default)
        app.MapDefaultOpenApi(developmentOnly: true);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseSessionRevocation();
        app.UseAuthorization();

        #endregion

        #region Endpoints

        // Map Aspire default health endpoints (/health, /alive, /ready)
        app.MapDefaultEndpoints();

        // Short login route - redirects to Keycloak
        app.MapGet("/login", (string? returnUrl) =>
        {
            var redirectUri = returnUrl ?? "/";
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        // Short logout route
        app.MapGet("/logout", async (HttpContext context, IOptions<AuthOptions> authOptions, string? returnUrl) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var keycloakBaseUrl = authOptions.Value.KeycloakBaseUrl;
            var realm = authOptions.Value.Realm;
            var clientId = authOptions.Value.ClientId;

            var keycloakLogoutUrl = $"{keycloakBaseUrl}/realms/{realm}/protocol/openid-connect/logout";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                keycloakLogoutUrl += $"?post_logout_redirect_uri={Uri.EscapeDataString(returnUrl)}&client_id={clientId}";
            }
            return Results.Redirect(keycloakLogoutUrl);
        });

        if (app.Environment.IsDevelopment())
        {
            // Root endpoint with available routes info
            app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: true))
                .AllowAnonymous();
        }

        #endregion

        #region Run Application

        app.Logger.LogInformation("Identity.Service starting...");
        app.Logger.LogInformation("Authority: {Authority}", app.Configuration["AuthOptions:Authority"]);
        app.Logger.LogInformation("ClientId: {ClientId}", app.Configuration["AuthOptions:ClientId"]);
        app.Logger.LogInformation("KeycloakBaseUrl: {KeycloakBaseUrl}", app.Configuration["AuthOptions:KeycloakBaseUrl"]);

        await app.RunAsync();

        #endregion
    }
}
