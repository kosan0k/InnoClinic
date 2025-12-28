using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Interceptors;

public class OutboxInsertInterceptor(IOutboxNotifier notifier) : SaveChangesInterceptor
{
    private readonly IOutboxNotifier _notifier = notifier;
    private bool _hasNewOutboxMessages;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {        
        _hasNewOutboxMessages = eventData.Context?.ChangeTracker
            .Entries<OutboxMessage>()
            .Any(e => e.State == EntityState.Added) ?? false;

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_hasNewOutboxMessages)
        {
            _notifier.NotifyNewMessage();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
