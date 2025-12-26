using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .AddDbContext<WriteDbContext>((provider, options) =>
                options
                    .UseNpgsql(writeConnectionString)
                    .AddInterceptors(provider.GetRequiredService<OutboxInsertInterceptor>()));

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

        return services;
    }

    private static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        // OutboxNotifier must be singleton - shared between OutboxService and OutboxProcessor
        // for the notification channel to work
        services.AddSingleton<OutboxNotifier>();
        services.AddSingleton<IOutboxNotifier>(sp => sp.GetRequiredService<OutboxNotifier>());
        
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddHostedService<OutboxProcessor>();        

        return services;
    }
}

