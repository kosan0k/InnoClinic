namespace Services.Shared.Messaging.Messages.Doctors;

public sealed record DoctorCreatedMessage
{
    public required Guid EventId { get; init; }
    public required DateTime OccurredOn { get; init; }

    public required Guid DoctorId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MiddleName { get; init; }
}
