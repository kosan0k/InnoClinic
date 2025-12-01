using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Services.Identity.Configurations;
using Services.Identity.Constants;
using Services.Identity.Data;
using Services.Identity.Extensions.Infrastructure;
using Services.Identity.Middleware;
using Services.Shared.Configuration;

namespace Services.Identity.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ====================================
        // Aspire Service Defaults
        // ====================================
        builder.AddServiceDefaults();

        // ====================================
        // Aspire Resource Connections
        // ====================================
        
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

        // ====================================
        // Configuration Binding
        // ====================================
        var authOptions = builder.Configuration
            .GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);
        
        var redisOptions = builder.Configuration
            .GetOptions<RedisOptions>(AuthConstants.ConfigSections.RedisOptions);

        // Register options with IOptions<T> pattern
        builder.Services.Configure<AuthOptions>(
            builder.Configuration.GetSection(AuthConstants.ConfigSections.AuthOptions));
        builder.Services.Configure<RedisOptions>(
            builder.Configuration.GetSection(AuthConstants.ConfigSections.RedisOptions));

        // ====================================
        // Keycloak Authentication (JWT Bearer + OpenID Connect)
        // ====================================
        
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
        builder.Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var authority = configuration["AuthOptions:Authority"];
                var clientId = configuration["AuthOptions:ClientId"];
                var clientSecret = configuration["AuthOptions:ClientSecret"];
                
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

        // ====================================
        // Core Services
        // ====================================
        
        // Add Auth services (session management, identity service)
        builder.Services.AddAuthServicesWithAspire(authOptions, redisOptions);

        // Add controllers
        builder.Services.AddControllers();

        // Add OpenAPI/Swagger
        builder.Services.AddOpenApi();

        // ====================================
        // Build Application
        // ====================================
        var app = builder.Build();

        // ====================================
        // Database Migration (Development)
        // ====================================
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
                // In dev, try to create the database if it doesn't exist
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

        // ====================================
        // Middleware Pipeline
        // ====================================
        
        // Development-only middleware
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // HTTPS redirection (disable in dev if using HTTP)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Authentication & Authorization
        app.UseAuthentication();
        
        // Session revocation check (after authentication, before authorization)
        app.UseSessionRevocation();
        
        app.UseAuthorization();

        // ====================================
        // Endpoints
        // ====================================
        
        // Map Aspire default health endpoints (/health, /alive, /ready)
        app.MapDefaultEndpoints();

        // Map controllers
        app.MapControllers();

        // ====================================
        // Convenience Routes
        // ====================================
        
        // Short login route - redirects to Keycloak
        app.MapGet("/login", (string? returnUrl) =>
        {
            var redirectUri = returnUrl ?? "/";
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        // Short logout route
        app.MapGet("/logout", async (HttpContext context, IConfiguration configuration, string? returnUrl) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var keycloakBaseUrl = configuration["AuthOptions:KeycloakBaseUrl"];
            var realm = configuration["AuthOptions:Realm"];
            var clientId = configuration["AuthOptions:ClientId"];
            var keycloakLogoutUrl = $"{keycloakBaseUrl}/realms/{realm}/protocol/openid-connect/logout";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                keycloakLogoutUrl += $"?post_logout_redirect_uri={Uri.EscapeDataString(returnUrl)}&client_id={clientId}";
            }
            return Results.Redirect(keycloakLogoutUrl);
        });

        // Root endpoint with available routes info
        app.MapGet("/", () => Results.Ok(new
        {
            service = "InnoClinic Identity Service",
            version = "1.0.0",
            endpoints = new
            {
                login = "/login",
                logout = "/logout",
                authStatus = "/api/auth/status",
                health = "/health",
                openapi = "/openapi/v1.json"
            }
        })).AllowAnonymous();

        // ====================================
        // Run Application
        // ====================================
        app.Logger.LogInformation("Identity.Service starting...");
        app.Logger.LogInformation("Authority: {Authority}", app.Configuration["AuthOptions:Authority"]);
        app.Logger.LogInformation("ClientId: {ClientId}", app.Configuration["AuthOptions:ClientId"]);
        app.Logger.LogInformation("KeycloakBaseUrl: {KeycloakBaseUrl}", app.Configuration["AuthOptions:KeycloakBaseUrl"]);
        
        await app.RunAsync();
    }
}
