using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Domain.Entities;

public record Doctor : ISoftDeletable
{
    public Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? MiddleName { get; init; }

    public DateTime DateOfBirth { get; init; }

    public required string Email { get; init; }

    public string? PhotoUrl { get; init; }

    public int CareerStartYear { get; init; }

    public DoctorStatus Status { get; init; } = DoctorStatus.AtWork;

    /// <summary>
    /// Indicates whether the doctor has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// The date and time when the doctor was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>
    /// Foreign key to the Specialization entity.
    /// </summary>
    public Guid SpecializationId { get; init; }

    /// <summary>
    /// Navigation property to the Specialization entity.
    /// Used for loading specialization details when needed.
    /// </summary>
    public Specialization? Specialization { get; init; }

    public static Doctor Create(
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string email,
        string? photoUrl,
        int careerStartYear,
        Guid specializationId,
        DoctorStatus status = DoctorStatus.AtWork)
    {
        return new Doctor
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = dateOfBirth,
            Email = email,
            PhotoUrl = photoUrl,
            CareerStartYear = careerStartYear,
            SpecializationId = specializationId,
            Status = status
        };
    }

    public Doctor WithStatus(DoctorStatus newStatus)
    {
        return this with { Status = newStatus };
    }

    public Doctor Update(
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string? photoUrl,
        int careerStartYear,
        Guid specializationId,
        DoctorStatus status)
    {
        return this with
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = dateOfBirth,
            PhotoUrl = photoUrl,
            CareerStartYear = careerStartYear,
            SpecializationId = specializationId,
            Status = status
        };
    }
}
