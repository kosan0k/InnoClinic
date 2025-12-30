using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that converts delete operations on entities implementing ISoftDeletable
/// into soft delete operations by setting IsDeleted and DeletedAt properties.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public SoftDeleteInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void ConvertDeleteToSoftDelete(DbContext context)
    {
        var deletedEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            entry.State = EntityState.Modified;

            var deletedAt = _timeProvider.GetUtcNow().UtcDateTime;

            SetSoftDeleteProperties(entry, deletedAt);
        }
    }

    private static void SetSoftDeleteProperties(EntityEntry entry, DateTime deletedAt)
    {
        // For records/classes with init-only setters, we need to use EF Core's property access
        entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
        entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = deletedAt;
    }
}
