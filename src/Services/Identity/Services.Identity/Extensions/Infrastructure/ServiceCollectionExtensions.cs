using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    /// Registers the local user database context and service.
    /// </summary>
    public static IServiceCollection AddLocalUserManagement(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<ILocalUserService, LocalUserService>();

        return services;
    }

    /// <summary>
    /// Registers all Auth.Service dependencies.
    /// </summary>
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        AuthOptions authOptions,
        RedisOptions redisOptions,
        string connectionString)
    {
        // Register options
        services.AddSingleton(Options.Create(authOptions));
        services.AddSingleton(Options.Create(redisOptions));

        // Add HTTP client factory
        services.AddHttpClient();

        // Add authentication
        services.AddJwtBearerAuthentication(authOptions);

        // Add Redis session management
        services.AddRedisSessionManagement(redisOptions);

        // Add Identity Service (Keycloak Admin API)
        services.AddIdentityService();

        // Add local user management
        services.AddLocalUserManagement(connectionString);

        return services;
    }
}

