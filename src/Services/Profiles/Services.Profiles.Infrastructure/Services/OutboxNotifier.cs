using System.Threading.Channels;
using Services.Profiles.Application.Common.Interfaces;

namespace Services.Profiles.Infrastructure.Services;

/// <summary>
/// Provides async signaling between OutboxService and OutboxProcessor.
/// Uses a bounded channel with drop-write semantics to coalesce rapid notifications.
/// </summary>
public sealed class OutboxNotifier : IOutboxNotifier, IDisposable
{
    private readonly Channel<byte> _channel;

    public OutboxNotifier()
    {
        // Bounded channel with capacity 1:
        // - DropWrite mode: if channel is full, new writes are silently dropped
        // - This is fine because we process ALL pending messages on each wake-up
        // - Multiple rapid notifications coalesce into a single wake-up
        _channel = Channel.CreateBounded<byte>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <inheritdoc />
    public void NotifyNewMessage()
    {
        // TryWrite returns false if channel is full (which is fine - processor will wake up anyway)
        _channel.Writer.TryWrite(0);
    }

    /// <summary>
    /// Gets the channel reader for the OutboxProcessor to await notifications.
    /// </summary>
    internal ChannelReader<byte> Reader => _channel.Reader;

    public void Dispose()
    {
        _channel.Writer.Complete();
    }
}

