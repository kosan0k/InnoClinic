using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Persistence;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Services.Profiles.Infrastructure.Services;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxNotifier _notifier;
    private readonly ILogger<OutboxProcessor> _logger;
    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxRetryCount = 3;

    private string _fullTableName = string.Empty;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        OutboxNotifier notifier,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>
    /// The main loop is now a linear pipeline of Results.
    /// No try-catch blocks here; errors flow to OnFailure.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started.");

        using (var setupScope = _scopeFactory.CreateScope())
        {
            var context = setupScope.ServiceProvider.GetRequiredService<WriteDbContext>();
            _fullTableName = ResolveTableName(context);
            _logger.LogInformation("Outbox Processor mapped to table: {TableName}", _fullTableName);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await ProcessBatchAsync(stoppingToken)
                .Bind(async hasProcessedWork =>
                    hasProcessedWork
                        ? UnitResult.Success<Exception>() // Work found? Loop immediately.
                        : await WaitForSignalOrTimeoutAsync(stoppingToken) // No work? Wait.
                );

            if (result.IsFailure)
            {
                _logger.LogError(result.Error, "Fatal error in Outbox loop. Pausing.");
                // Manual delay on catastrophic failure (e.g., DB down) to prevent log flooding
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private static string ResolveTableName(WriteDbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage is not registered in DbContext");

        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName();

        // Safe quoting for Postgres to strictly match casing
        return string.IsNullOrEmpty(schema)
            ? $@"""{tableName}"""
            : $@"""{schema}"".""{tableName}""";
    }

    /// <summary>
    /// Wraps the complex DB Transaction logic in a Result.
    /// Returns Success(true) if work was done, Success(false) if queue was empty.
    /// Returns Failure(Exception) if DB connection/commit fails.
    /// </summary>
    private async Task<Result<bool, Exception>> ProcessBatchAsync(CancellationToken ct)
    {
        return await Result.Try(
            func: async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
                
                return await ProcessTransactionBatchAsync(dbContext, publisher, ct);
            },
            errorHandler: ex => new Exception("Error processing outbox messages", ex));
    }

    private async Task<bool> ProcessTransactionBatchAsync(
        WriteDbContext dbContext,
        IPublisher publisher,
        CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        
        var sql = $@"
            SELECT * FROM {_fullTableName}
            WHERE ""ProcessedOn"" IS NULL
            ORDER BY ""OccurredOn""
            LIMIT {BatchSize}
            FOR UPDATE SKIP LOCKED";

        var messages = await dbContext.OutboxMessages
            .FromSqlRaw(sql)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return false;

        foreach (var message in messages)
        {
            var updatedMessage = await ProcessSingleMessageAsync(message, publisher, ct);
            dbContext.Entry(message).CurrentValues.SetValues(updatedMessage);
        }

        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    /// <summary>
    /// Wraps the waiting logic. 
    /// Returns Success when signal received OR timeout occurs (both are valid flow states).
    /// Returns Failure only if the channel reader itself crashes (unlikely).
    /// </summary>
    private async ValueTask<UnitResult<Exception>> WaitForSignalOrTimeoutAsync(CancellationToken ct)
    {
        UnitResult<Exception> result;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_pollingInterval);

        try
        {
            await _notifier.Reader.ReadAsync(cts.Token);
            result = UnitResult.Success<Exception>();
        }
        catch (OperationCanceledException)
        {
            // Timeout is not an error; it's a valid "Wait Finished" state.
            result = UnitResult.Success<Exception>();
        }
        catch (Exception ex)
        {
            result = ex;
        }

        return result;
    }

    /// <summary>
    /// Pure functional pipeline for a single message.
    /// </summary>
    private async ValueTask<OutboxMessage> ProcessSingleMessageAsync(
        OutboxMessage message,
        IPublisher publisher,
        CancellationToken ct)
    {
        var pipelineResult = await GetEventType(message)
            .Bind(type => DeserializeEvent(message.Payload, type))
            .Bind(async domainEvent => await PublishEvent(domainEvent, publisher, ct));

        return pipelineResult.Match(
            onSuccess: () => MarkAsSuccess(message),
            onFailure: (exception) => MarkAsFailure(message, exception)
        );
    }

    private static Result<Type, Exception> GetEventType(OutboxMessage message)
    {
        try
        {
            if (_typeCache.TryGetValue(message.EventType, out var cachedType))
            {
                return Result.Success<Type, Exception>(cachedType);
            }

            var type = Type.GetType(message.EventType);
            
            type ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == message.EventType || t.Name == message.EventType);

            if (type is not null)
            {
                // Cache the result for next time
                _typeCache[message.EventType] = type;
                return Result.Success<Type, Exception>(type);
            }
            else
            {
                return new InvalidOperationException($"Type '{message.EventType}' could not be found in any loaded assembly.");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<Type, Exception>(ex);
        }
    }

    private static Result<INotification, Exception> DeserializeEvent(string payload, Type type)
    {
        try
        {
            var domainEvent = JsonSerializer.Deserialize(payload, type) as INotification;

            return domainEvent is not null
                ? Result.Success<INotification, Exception>(domainEvent)
                : Result.Failure<INotification, Exception>(new InvalidOperationException("Payload is not a valid INotification"));
        }
        catch (Exception ex)
        {
            return Result.Failure<INotification, Exception>(ex);
        }
    }

    private static async ValueTask<UnitResult<Exception>> PublishEvent(
        INotification domainEvent,
        IPublisher publisher,
        CancellationToken ct)
    {
        try
        {
            await publisher.Publish(domainEvent, ct);
            return UnitResult.Success<Exception>();
        }
        catch (Exception ex)
        {
            return new Exception("Error on publishing event", ex);
        }
    }

    private static OutboxMessage MarkAsSuccess(OutboxMessage message) =>
        message with { ProcessedOn = DateTime.UtcNow, Error = null };

    private OutboxMessage MarkAsFailure(OutboxMessage message, Exception ex)
    {
        var nextRetryCount = message.RetryCount + 1;
        var isPoisonPill = nextRetryCount >= MaxRetryCount;

        if (isPoisonPill)
            _logger.LogError(ex, "Message {Id} dead-lettered.", message.Id);
        else
            _logger.LogWarning(ex, "Message {Id} failed. Retry {Count}.", message.Id, nextRetryCount);

        return message with
        {
            RetryCount = nextRetryCount,
            Error = ex.Message,
            ProcessedOn = isPoisonPill ? DateTime.UtcNow : null
        };
    }
}

