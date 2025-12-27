using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.Create;

public sealed class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Guid>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly ISpecializationRepository _specializationRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        ISpecializationRepository specializationRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _specializationRepository = specializationRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        // Validate that the specialization exists
        var specializationExists = await _specializationRepository.ExistsAsync(
            request.SpecializationId, 
            cancellationToken);

        if (!specializationExists)
        {
            throw new NotFoundException(
                nameof(Specialization), 
                request.SpecializationId.ToString());
        }

        var doctor = Doctor.Create(
            firstName: request.FirstName,
            lastName: request.LastName,
            middleName: request.MiddleName,
            dateOfBirth: request.DateOfBirth,
            email: request.Email,
            photoUrl: request.PhotoUrl,
            careerStartYear: request.CareerStartYear,
            specializationId: request.SpecializationId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _doctorRepository.AddAsync(doctor, cancellationToken);

            var domainEvent = new DoctorCreatedEvent
            {
                DoctorId = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                MiddleName = doctor.MiddleName,
                DateOfBirth = doctor.DateOfBirth,
                Email = doctor.Email,
                PhotoUrl = doctor.PhotoUrl,
                CareerStartYear = doctor.CareerStartYear,
                SpecializationId = doctor.SpecializationId,
                Status = doctor.Status
            };

            await _outboxService.AddMessageAsync(domainEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return doctor.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
