namespace Services.Profiles.Domain.Entities;

/// <summary>
/// Interface for entities that support soft delete functionality.
/// Entities implementing this interface will be marked as deleted instead of being physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// The date and time when the entity was soft deleted.
    /// </summary>
    DateTime? DeletedAt { get; }
}

