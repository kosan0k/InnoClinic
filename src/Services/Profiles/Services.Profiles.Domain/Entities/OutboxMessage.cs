namespace Services.Profiles.Domain.Entities;

public record OutboxMessage
{
    public Guid Id { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public DateTime OccurredOn { get; init; }
    public DateTime? ProcessedOn { get; init; }
    public string? Error { get; init; }
    public int RetryCount { get; init; }
}

