using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Infrastructure.Services;

namespace Services.Profiles.Infrastructure.Persistence.Interceptors;

public class OutboxInsertInterceptor(OutboxNotifier notifier) : SaveChangesInterceptor
{
    private readonly OutboxNotifier _notifier = notifier;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // Check if the ChangeTracker has any added OutboxMessages
        var hasOutboxMessages = eventData.Context?
            .ChangeTracker
            .Entries<OutboxMessage>()
            .Any(e => e.State == EntityState.Added || e.State == EntityState.Modified) ?? false;

        if (result > 0 && hasOutboxMessages)
        {
            _notifier.NotifyNewMessage();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
