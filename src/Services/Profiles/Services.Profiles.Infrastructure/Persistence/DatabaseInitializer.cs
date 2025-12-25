using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Services.Profiles.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabasesAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WriteDbContext>>();

        await InitializeWriteDatabaseAsync(scope.ServiceProvider, logger, cancellationToken);
        await InitializeReadDatabaseAsync(scope.ServiceProvider, logger, cancellationToken);
    }

    private static async Task InitializeWriteDatabaseAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var writeContext = serviceProvider.GetRequiredService<WriteDbContext>();
            
            logger.LogInformation("Applying migrations to write database...");
            await writeContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Write database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the write database");
            throw;
        }
    }

    private static async Task InitializeReadDatabaseAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var readContext = serviceProvider.GetRequiredService<ReadDbContext>();
            
            logger.LogInformation("Applying migrations to read database...");
            await readContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Read database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the read database");
            throw;
        }
    }
}

