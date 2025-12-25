using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Persistence;

namespace Services.Profiles.Infrastructure.Services;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OutboxNotifier _notifier;
    private readonly ILogger<OutboxProcessor> _logger;
    
    // Fallback polling interval - used when no notifications are received
    // This ensures reliability even if notifications are missed
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
    
    private const int BatchSize = 20;
    private const int MaxRetryCount = 3;

    private static readonly Dictionary<string, Type> EventTypes = new()
    {
        [nameof(DoctorCreatedEvent)] = typeof(DoctorCreatedEvent),
        [nameof(DoctorUpdatedEvent)] = typeof(DoctorUpdatedEvent),
        [nameof(DoctorStatusChangedEvent)] = typeof(DoctorStatusChangedEvent)
    };

    public OutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        OutboxNotifier notifier,
        ILogger<OutboxProcessor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started (hybrid mode: notification + {PollingInterval}s fallback polling)", 
            _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either:
                // 1. A notification that new messages are available (immediate processing)
                // 2. The polling interval timeout (fallback for reliability)
                await WaitForWorkAsync(stoppingToken);
                
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
                
                // Brief delay before retry to prevent tight error loops
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    /// <summary>
    /// Waits for either a notification or the polling timeout, whichever comes first.
    /// This enables immediate processing on notification while maintaining reliability via polling.
    /// </summary>
    private async Task WaitForWorkAsync(CancellationToken stoppingToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(_pollingInterval);

        try
        {
            // Wait for notification (will throw OperationCanceledException on timeout)
            await _notifier.Reader.ReadAsync(timeoutCts.Token);
            _logger.LogDebug("Outbox processing triggered by notification");
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout reached, not a shutdown - proceed with polling-based processing
            _logger.LogDebug("Outbox processing triggered by polling interval");
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var writeContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        var readContext = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var messages = await writeContext.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetryCount)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, readContext, cancellationToken);

                var processedMessage = message with { ProcessedOn = DateTime.UtcNow };
                writeContext.Entry(message).CurrentValues.SetValues(processedMessage);
                
                _logger.LogInformation(
                    "Processed outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                var failedMessage = message with
                {
                    RetryCount = message.RetryCount + 1,
                    Error = ex.Message
                };
                writeContext.Entry(message).CurrentValues.SetValues(failedMessage);

                _logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId} of type {EventType}. Retry count: {RetryCount}",
                    message.Id,
                    message.EventType,
                    failedMessage.RetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await writeContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        ReadDbContext readContext,
        CancellationToken cancellationToken)
    {
        if (!EventTypes.TryGetValue(message.EventType, out var eventType))
        {
            _logger.LogWarning("Unknown event type: {EventType}", message.EventType);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);

        switch (domainEvent)
        {
            case DoctorCreatedEvent created:
                await HandleDoctorCreatedAsync(created, readContext, cancellationToken);
                break;

            case DoctorUpdatedEvent updated:
                await HandleDoctorUpdatedAsync(updated, readContext, cancellationToken);
                break;

            case DoctorStatusChangedEvent statusChanged:
                await HandleDoctorStatusChangedAsync(statusChanged, readContext, cancellationToken);
                break;
        }
    }

    private static async Task HandleDoctorCreatedAsync(
        DoctorCreatedEvent @event,
        ReadDbContext readContext,
        CancellationToken cancellationToken)
    {
        var doctor = new Doctor
        {
            Id = @event.DoctorId,
            FirstName = @event.FirstName,
            LastName = @event.LastName,
            MiddleName = @event.MiddleName,
            DateOfBirth = @event.DateOfBirth,
            Email = @event.Email,
            PhotoUrl = @event.PhotoUrl,
            CareerStartYear = @event.CareerStartYear,
            Status = @event.Status
        };

        await readContext.Doctors.AddAsync(doctor, cancellationToken);
        await readContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task HandleDoctorUpdatedAsync(
        DoctorUpdatedEvent @event,
        ReadDbContext readContext,
        CancellationToken cancellationToken)
    {
        var existingDoctor = await readContext.Doctors
            .FirstOrDefaultAsync(d => d.Id == @event.DoctorId, cancellationToken);

        if (existingDoctor is null)
        {
            return;
        }

        var updatedDoctor = new Doctor
        {
            Id = @event.DoctorId,
            FirstName = @event.FirstName,
            LastName = @event.LastName,
            MiddleName = @event.MiddleName,
            DateOfBirth = @event.DateOfBirth,
            Email = @event.Email,
            PhotoUrl = @event.PhotoUrl,
            CareerStartYear = @event.CareerStartYear,
            Status = @event.Status
        };

        readContext.Entry(existingDoctor).CurrentValues.SetValues(updatedDoctor);
        await readContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task HandleDoctorStatusChangedAsync(
        DoctorStatusChangedEvent @event,
        ReadDbContext readContext,
        CancellationToken cancellationToken)
    {
        var existingDoctor = await readContext.Doctors
            .FirstOrDefaultAsync(d => d.Id == @event.DoctorId, cancellationToken);

        if (existingDoctor is null)
        {
            return;
        }

        var updatedDoctor = existingDoctor with { Status = @event.NewStatus };
        readContext.Entry(existingDoctor).CurrentValues.SetValues(updatedDoctor);
        await readContext.SaveChangesAsync(cancellationToken);
    }
}

