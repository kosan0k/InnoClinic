using System.Text.Json;
using Services.Profiles.Application.Common.Events;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Persistence;

namespace Services.Profiles.Infrastructure.Services;

public sealed class OutboxService(WriteDbContext context) : IOutboxService
{
    private readonly WriteDbContext _context = context;

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
    }
}

