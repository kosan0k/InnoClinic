using MediatR;
using Services.Profiles.Domain.Enums;

namespace Services.Profiles.Features.Doctors.Commands.Edit;

public record EditDoctorCommand : IRequest
{
    public Guid Id { get; init; }

    public required string FirstName { get; init; }
    
    public required string LastName { get; init; } 

    public string? MiddleName { get; init; }
    
    public DateTime DateOfBirth { get; init; }
    
    public Guid SpecializationId { get; init; }

    public Guid OfficeId { get; init; }

    public int CareerStartYear { get; init; }

    public DoctorStatus Status { get; init; }
}
