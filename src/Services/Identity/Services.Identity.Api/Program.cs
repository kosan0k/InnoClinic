using Microsoft.EntityFrameworkCore;
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
        // Configuration Binding
        // ====================================
        var authOptions = builder.Configuration
            .GetOptions<AuthOptions>(AuthConstants.ConfigSections.AuthOptions);
        
        var redisOptions = builder.Configuration
            .GetOptions<RedisOptions>(AuthConstants.ConfigSections.RedisOptions);
        
        var connectionString = builder.Configuration
            .GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register options with IOptions<T> pattern
        builder.Services.Configure<AuthOptions>(
            builder.Configuration.GetSection(AuthConstants.ConfigSections.AuthOptions));
        builder.Services.Configure<RedisOptions>(
            builder.Configuration.GetSection(AuthConstants.ConfigSections.RedisOptions));

        // ====================================
        // Core Services
        // ====================================
        
        // Add all Auth.Service dependencies
        builder.Services.AddAuthServices(authOptions, redisOptions, connectionString);

        // Add controllers
        builder.Services.AddControllers();

        // Add OpenAPI/Swagger
        builder.Services.AddOpenApi();

        // ====================================
        // Health Checks
        // ====================================
        builder.Services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"])
            .AddRedis(redisOptions.ConnectionString, name: "redis", tags: ["cache", "ready"]);

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
            // Optional: Add Swagger UI
            // app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Auth.Service API v1"));
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
        
        // Health check endpoints
        app.MapHealthChecks($"/{AuthConstants.Routes.Health}", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        app.MapHealthChecks($"/{AuthConstants.Routes.Health}/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapHealthChecks($"/{AuthConstants.Routes.Health}/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false // Just check if the app is running
        });

        // Map controllers
        app.MapControllers();

        // ====================================
        // Run Application
        // ====================================
        app.Logger.LogInformation("Auth.Service starting...");
        app.Logger.LogInformation("Authority: {Authority}", authOptions.Authority);
        app.Logger.LogInformation("ClientId: {ClientId}", authOptions.ClientId);
        
        await app.RunAsync();
    }
}
