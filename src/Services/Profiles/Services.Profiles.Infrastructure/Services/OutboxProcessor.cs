using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Persistence;
using System.Text.Json;

namespace Services.Profiles.Infrastructure.Services;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxNotifier _notifier;
    private readonly ILogger<OutboxProcessor> _logger;

    // Configurable constants
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5); // Fallback loop
    private const int BatchSize = 20;
    private const int MaxRetryCount = 3;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        OutboxNotifier notifier,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Attempt to process a batch
                bool processedWork = await ProcessBatchAsync(stoppingToken);

                // If we processed work, check immediately for more (drain the queue).
                // If no work, wait for Signal OR Timeout (Polling).
                if (!processedWork)
                {
                    await WaitForSignalOrTimeoutAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Outbox loop");
                await Task.Delay(1000, stoppingToken); // Circuit breaker pause
            }
        }
    }

    private async Task WaitForSignalOrTimeoutAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_pollingInterval);
        try
        {
            await _notifier.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout meant no signal received; proceed to poll.
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // --- STEP A: Fetch & Lock (Concurrency Safety) ---
        // We use an explicit transaction to hold the 'SKIP LOCKED' row locks
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        // POSTGRESQL SYNTAX:
        var sql = $@"
            SELECT * FROM ""OutboxMessages""
            WHERE ""ProcessedOn"" IS NULL
            ORDER BY ""OccurredOn""
            LIMIT {BatchSize}
            FOR UPDATE SKIP LOCKED";

        // FOR SQL SERVER USE:
        // var sql = $@"
        //    SELECT TOP {BatchSize} * //    FROM OutboxMessages WITH (UPDLOCK, READPAST)
        //    WHERE ProcessedOn IS NULL
        //    ORDER BY OccurredOn";

        var messages = await dbContext.OutboxMessages
            .FromSqlRaw(sql)
            .ToListAsync(ct);

        if (messages.Count == 0)
        {
            return false;
        }

        // --- STEP B: Process & Clone (Immutability Handling) ---
        foreach (var message in messages)
        {
            OutboxMessage updatedMessage;

            try
            {
                // Dynamic Deserialization
                var eventType = Type.GetType(message.EventType) ?? throw new Exception($"Unknown type: {message.EventType}");
                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);

                // Dispatch via MediatR (Clean Architecture)
                if (domainEvent is INotification notification)
                {
                    await publisher.Publish(notification, ct);
                }

                // Success: Create NEW record state (since we can't edit the old one)
                updatedMessage = message with
                {
                    ProcessedOn = DateTime.UtcNow,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                // Failure: Create NEW record state with incremented retry
                var nextRetryCount = message.RetryCount + 1;

                updatedMessage = message with
                {
                    RetryCount = nextRetryCount,
                    Error = ex.ToString()
                };

                // If max retries exceeded, mark as processed (Poison Pill) to stop the loop
                if (nextRetryCount >= MaxRetryCount)
                {
                    updatedMessage = updatedMessage with { ProcessedOn = DateTime.UtcNow };
                    _logger.LogError(ex, "Message {Id} reached max retries and was dead-lettered.", message.Id);
                }
                else
                {
                    _logger.LogWarning(ex, "Message {Id} failed. Retry {Count}/{Max}.", message.Id, nextRetryCount, MaxRetryCount);
                }
            }

            // Update EF Change Tracker ---            
            dbContext.Entry(message).CurrentValues.SetValues(updatedMessage);
        }

        // Commit
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }
}

