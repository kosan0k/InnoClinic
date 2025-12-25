namespace Services.Profiles.Application.Common.Interfaces;

/// <summary>
/// Notifies the outbox processor that new messages are available for processing.
/// Used to enable immediate processing instead of waiting for the polling interval.
/// </summary>
public interface IOutboxNotifier
{
    /// <summary>
    /// Signals that a new outbox message has been added and should be processed.
    /// </summary>
    void NotifyNewMessage();
}

