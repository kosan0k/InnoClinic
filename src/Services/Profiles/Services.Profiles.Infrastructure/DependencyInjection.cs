using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Infrastructure.Persistence;
using Services.Profiles.Infrastructure.Persistence.Interceptors;
using Services.Profiles.Infrastructure.Persistence.Repositories;
using Services.Profiles.Infrastructure.Services;

namespace Services.Profiles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWriteDatabase(configuration);
        services.AddReadDatabase(configuration);
        services.AddRepositories();
        services.AddOutboxServices();
        services.AddMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddWriteDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var writeConnectionString = configuration.GetConnectionString("profiles-write-db")
            ?? throw new InvalidOperationException("profiles-write-db connection string is not configured");

        services
            .AddSingleton<OutboxInsertInterceptor>()
            .AddSingleton<SoftDeleteInterceptor>()
            .AddDbContext<WriteDbContext>((provider, options) =>
                options
                    .UseNpgsql(writeConnectionString)
                    .AddInterceptors(
                        provider.GetRequiredService<SoftDeleteInterceptor>(),
                        provider.GetRequiredService<OutboxInsertInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WriteDbContext>());

        return services;
    }

    private static IServiceCollection AddReadDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var readConnectionString = configuration.GetConnectionString("profiles-read-db")
            ?? throw new InvalidOperationException("profiles-read-db connection string is not configured");

        services
            .AddDbContext<ReadDbContext>(options =>
                options
                    .UseNpgsql(readConnectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking))
            .AddScoped<IDoctorProjectionWriter, DoctorProjectionWriter>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IDoctorWriteRepository, DoctorWriteRepository>();
        services.AddScoped<IDoctorReadRepository, DoctorReadRepository>();
        services.AddScoped<ISpecializationRepository, SpecializationRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        return services;
    }

    private static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        // OutboxNotifier must be singleton - shared between OutboxService and OutboxProcessor
        // for the reactive notification stream to work
        services.AddSingleton<IOutboxNotifier, OutboxNotifier>();
        
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddHostedService<OutboxProcessor>();        

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("rabbitmq")
            ?? throw new InvalidOperationException("rabbitmq connection string is not configured");

        services.AddSingleton<IConnection>(_ =>
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                AutomaticRecoveryEnabled = true
            };

            return factory.CreateConnection();
        });

        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}
