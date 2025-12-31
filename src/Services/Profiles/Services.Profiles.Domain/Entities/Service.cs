namespace Services.Profiles.Domain.Entities;

/// <summary>
/// Represents a medical service that can be provided by specializations.
/// Stored in a separate table for extensibility.
/// </summary>
public record Service
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Indicates if this service is currently active and available for assignment.
    /// </summary>
    public bool IsActive { get; init; } = true;

    public static Service Create(string name, bool isActive = true)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive
        };
    }

    public static Service Create(Guid id, string name, bool isActive = true)
    {
        return new Service
        {
            Id = id,
            Name = name,
            IsActive = isActive
        };
    }
}

