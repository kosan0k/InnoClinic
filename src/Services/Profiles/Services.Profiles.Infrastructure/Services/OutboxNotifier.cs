using System.Reactive;
using System.Reactive.Subjects;
using Services.Profiles.Application.Common.Interfaces;

namespace Services.Profiles.Infrastructure.Services;

/// <summary>
/// Provides reactive signaling between OutboxService and OutboxProcessor.
/// Uses a Subject to push notifications that new outbox messages are ready for processing.
/// </summary>
public sealed class OutboxNotifier : IOutboxNotifier, IDisposable
{
    private readonly Subject<Unit> _subject = new();

    /// <inheritdoc />
    public IObservable<Unit> Notifications => _subject;

    /// <inheritdoc />
    public void NotifyNewMessage()
    {
        _subject.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}
