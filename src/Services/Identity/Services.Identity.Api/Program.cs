using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.Identity.Features.Auth;
using Services.Identity.Features.Auth.Registration;
using Services.Identity.Features.Session.Middleware;
using Services.Identity.Features.Session.Registration;
using Services.Identity.Features.Users;
using Services.Identity.Features.Users.Registration;
using Services.Identity.Shared.Configurations;
using Services.Identity.Shared.Costants;
using Services.Shared.Configuration;
using StackExchange.Redis;

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

        // Add Redis using Aspire's connection handling
        builder.AddRedisClient("redis");

        // Get Redis connection for shared Data Protection
        var redisConnectionString = builder.Configuration.GetConnectionString("redis")
            ?? throw new InvalidOperationException("Redis connection string is not configured");
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        #endregion

        #region Configuration Binding

        var authOptions = builder.Configuration
            .GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);

        var redisOptions = builder.Configuration
            .GetOptions<RedisOptions>(AuthConstants.ConfigSections.RedisOptions);

        // Register options
        builder.Services
            .AddSingleton(Options.Create(authOptions))
            .AddSingleton(Options.Create(redisOptions));

        // Register HttpClientFactory for token refresh requests
        builder.Services.AddHttpClient("KeycloakTokenClient", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        #endregion

        builder.Services
            .ConfigureOpenIdAuthentication(
                configuration: builder.Configuration,
                redisConnection: redisConnection,
                isDevelopment: builder.Environment.IsDevelopment())
            .UseKeycloakIdentityService()
            .UseSessionRevocation()
            .AddAuthorization(options =>
            {
                // Define a policy named "AdminsOnly"
                options.AddPolicy("AdminsOnly", policy =>
                    policy.RequireRole("admin"));
            });

        // Add OpenAPI with Scalar UI
        builder.AddDefaultOpenApi(
            title: "InnoClinic Identity API",
            version: "v1",
            description: "Authentication and identity management service for InnoClinic platform");

        var app = builder.Build();

        #region Middleware Pipeline        

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            // Map OpenAPI and Scalar UI (development only by default)
            app.MapDefaultOpenApi(developmentOnly: true);
        }

        app.UseAuthentication();
        app.UseSessionRevocation();
        app.UseAuthorization();

        #endregion

        #region Endpoints

        // Map Aspire default health endpoints (/health, /alive, /ready)
        app.MapDefaultEndpoints();

        var authGroup = app.MapGroup("/auth");

        authGroup.MapGet("/login", AuthActions.LoginAsync)
            .AllowAnonymous();

        authGroup.MapGet("/logout", AuthActions.LogoutAsync)
            .RequireAuthorization(new AuthorizeAttribute 
            {
                AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme
            });

        authGroup.MapGet("/tokens", TokenActions.GetTokensAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme
            })
            .WithDescription("Get current access and refresh tokens from authenticated session");

        authGroup.MapPost("/refresh", TokenActions.RefreshTokenAsync)
            .AllowAnonymous()
            .WithDescription("Refresh access token using a refresh token");

        app.MapPost("/users", UsersActions.RegisterUserAsync)
            .RequireAuthorization(new AuthorizeAttribute(policy: "AdminsOnly")
            {
                AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme
            });

        #endregion

        #region Run Application

        app.Logger.LogInformation("Identity.Service starting...");
        app.Logger.LogInformation("Authority: {Authority}", authOptions.Authority);
        app.Logger.LogInformation("ClientId: {ClientId}", authOptions.ClientId);
        app.Logger.LogInformation("KeycloakBaseUrl: {KeycloakBaseUrl}", authOptions.KeycloakBaseUrl);

        await app.RunAsync();

        #endregion
    }
}
