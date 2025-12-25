using Services.Profiles.Application.Common.Events;

namespace Services.Profiles.Application.Common.Interfaces;

public interface IOutboxService
{
    Task AddMessageAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

