using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.Identity.Authentication;
using Services.Identity.Configurations;
using Services.Identity.Data;
using Services.Identity.Services;
using StackExchange.Redis;

namespace Services.Identity.Extensions.Infrastructure;

/// <summary>
/// Extension methods for registering Auth.Service dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Redis connection and session revocation service.
    /// </summary>
    public static IServiceCollection AddRedisSessionManagement(
        this IServiceCollection services,
        RedisOptions redisOptions)
    {
        // Register Redis connection multiplexer as singleton
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = 3;
            configuration.ConnectTimeout = 5000;
            
            return ConnectionMultiplexer.Connect(configuration);
        });

        // Register distributed cache (optional, for general caching)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        // Register session revocation service
        services.AddScoped<ISessionRevocationService, SessionRevocationService>();

        return services;
    }

    /// <summary>
    /// Registers the Identity Service for Keycloak Admin API operations.
    /// </summary>
    public static IServiceCollection AddIdentityService(this IServiceCollection services)
    {
        services.AddHttpClient<IIdentityService, IdentityService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Registers Auth.Service dependencies when using .NET Aspire.
    /// Database context and Redis are handled by Aspire's resource management.
    /// </summary>
    public static IServiceCollection AddAuthServicesWithAspire(this IServiceCollection services)
    {
        // Add HTTP client factory
        services.AddHttpClient();

        // Register JWT Bearer events handler
        services.AddScoped<KeycloakJwtBearerEvents>();

        // Add Identity Service (Keycloak Admin API)
        services.AddIdentityService();

        // Register session revocation service (uses IConnectionMultiplexer from Aspire)
        services.AddScoped<ISessionRevocationService, SessionRevocationService>();

        // Register local user service (uses DbContext from Aspire)
        services.AddScoped<ILocalUserService, LocalUserService>();

        return services;
    }
}

