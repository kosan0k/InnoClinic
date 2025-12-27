using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Flattened read model for Doctor queries in the CQRS read database.
/// Contains denormalized specialization data to avoid joins during queries.
/// </summary>
public sealed class DoctorReadModel
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? MiddleName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public required string Email { get; set; }

    public string? PhotoUrl { get; set; }

    public int CareerStartYear { get; set; }

    public DoctorStatus Status { get; set; } = DoctorStatus.AtWork;

    /// <summary>
    /// Foreign key to the Specialization (stored for reference but not used for joins in queries).
    /// </summary>
    public Guid SpecializationId { get; set; }

    /// <summary>
    /// Denormalized specialization name for direct query access.
    /// </summary>
    public required string SpecializationName { get; set; }

    /// <summary>
    /// Denormalized list of service names for direct query access.
    /// Stored as PostgreSQL text array for efficient querying without joins.
    /// </summary>
    public required List<string> Services { get; set; }
}

