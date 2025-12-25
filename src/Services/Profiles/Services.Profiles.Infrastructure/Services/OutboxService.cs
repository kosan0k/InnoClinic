using System.Text.Json;
using Services.Profiles.Application.Common.Events;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Persistence;

namespace Services.Profiles.Infrastructure.Services;

public sealed class OutboxService : IOutboxService
{
    private readonly WriteDbContext _context;
    private readonly IOutboxNotifier _notifier;

    public OutboxService(WriteDbContext context, IOutboxNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task AddMessageAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            OccurredOn = integrationEvent.OccurredOn,
            ProcessedOn = null,
            Error = null,
            RetryCount = 0
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        
        // Notify the processor that a new message is available
        // This triggers immediate processing instead of waiting for the polling interval
        _notifier.NotifyNewMessage();
    }
}

