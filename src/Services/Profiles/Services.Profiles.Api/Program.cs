using System.Security.Claims;
using Services.Profiles.Application;
using Services.Profiles.Infrastructure;
using Services.Profiles.Infrastructure.Persistence;
using Services.Shared.Authentication;
using StackExchange.Redis;

namespace Services.Profiles.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add Redis for shared Data Protection
        builder.AddRedisClient("redis");

        // Add services to the container.
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        #region Authentication & Authorization

        // Get configuration from environment (set by Aspire)
        var keycloakBaseUrl = builder.Configuration["AuthOptions:KeycloakBaseUrl"] 
            ?? throw new InvalidOperationException("AuthOptions:KeycloakBaseUrl is not configured");
        var keycloakRealm = builder.Configuration["AuthOptions:Realm"] ?? "AppRealm";
        var authority = $"{keycloakBaseUrl}/realms/{keycloakRealm}";
        
        var identityLoginUrl = builder.Configuration["AuthOptions:IdentityLoginUrl"] 
            ?? throw new InvalidOperationException("AuthOptions:IdentityLoginUrl is not configured");

        // Get Redis connection for shared Data Protection
        var redisConnectionString = builder.Configuration.GetConnectionString("redis") 
            ?? throw new InvalidOperationException("Redis connection string is not configured");
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        builder.Services.AddKeycloakAuthentication(
            authority: authority,
            identityLoginUrl: identityLoginUrl,
            redisConnection: redisConnection,
            isDevelopment: builder.Environment.IsDevelopment());

        builder.Services.AddAuthorization(options =>
        {
            // Use RequireClaim with explicit ClaimTypes.Role to ensure consistent behavior
            // across both JWT Bearer and Cookie authentication schemes
            options.AddPolicy("AdminsOnly", policy =>
                policy.RequireClaim(ClaimTypes.Role, "admin"));
        });

        #endregion

        builder.Services.AddControllers();
        
        // Add OpenAPI with JWT Bearer security scheme for Scalar UI
        builder.AddDefaultOpenApi(
            title: "InnoClinic Profiles API",
            version: "v1",
            description: "Profiles management service for InnoClinic platform - Doctors, Patients, Receptionists");

        var app = builder.Build();

        // Initialize databases (apply migrations)
        if (app.Environment.IsDevelopment())
        {
            await DatabaseInitializer.InitializeDatabasesAsync(app.Services);
        }

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapDefaultOpenApi(developmentOnly: true);
            app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}
