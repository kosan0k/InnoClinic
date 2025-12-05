using Microsoft.AspNetCore.Authentication.Cookies;
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

        #endregion

        builder.Services
            .ConfigureOpenIdAuthentication(
                configuration: builder.Configuration,
                isDevelopment: builder.Environment.IsDevelopment())
            .UseKeycloakIdentityService()
            .UseSessionRevocation()
            .AddAuthorization();

        // Add OpenAPI with Scalar UI
        builder.AddDefaultOpenApi(
            title: "InnoClinic Identity API",
            version: "v1",
            description: "Authentication and identity management service for InnoClinic platform");

        var app = builder.Build();

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

        var authGroup = app.MapGroup("/auth");

        authGroup.MapGet("/login", AuthActions.LoginAsync)
            .AllowAnonymous();

        authGroup.MapGet("/logout", AuthActions.LogoutAsync)
            .RequireAuthorization(new AuthorizeAttribute 
            {
                AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme
            });

        app.MapPost("/users", UsersActions.RegisterUserAsync)
            .AllowAnonymous(); //TODO : Change to RequireAuthorization when only admins can create users

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
