namespace Services.Profiles.Domain.Entities;

/// <summary>
/// Represents a medical specialization with associated services.
/// </summary>
public record Specialization
{
    private readonly List<Service> _services = [];

    public Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Indicates if this specialization is currently active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Collection of services available for this specialization.
    /// Managed via a join table for extensibility.
    /// </summary>
    public IReadOnlyList<Service> Services => _services;

    public static Specialization Create(string name, bool isActive = true)
    {
        return new Specialization
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive
        };
    }

    public static Specialization Create(Guid id, string name, bool isActive = true)
    {
        return new Specialization
        {
            Id = id,
            Name = name,
            IsActive = isActive
        };
    }
}
