using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.Edit;

public sealed class EditDoctorCommandHandler : IRequestHandler<EditDoctorCommand, UnitResult<Exception>>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly ISpecializationRepository _specializationRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public EditDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        ISpecializationRepository specializationRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _doctorRepository = doctorRepository;
        _specializationRepository = specializationRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<UnitResult<Exception>> Handle(EditDoctorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var doctor = await _doctorRepository.GetByIdAsync(request.Id, cancellationToken);
            
            if (doctor is null)
            {
                return new NotFoundException(nameof(Doctor), request.Id);
            }

            // Validate that the specialization exists
            var specializationExists = await _specializationRepository.ExistsAsync(
                request.SpecializationId, 
                cancellationToken);

            if (!specializationExists)
            {
                return UnitResult.Failure<Exception>(
                    new NotFoundException(nameof(Specialization), request.SpecializationId.ToString()));
            }

            var updatedDoctor = doctor.Update(
                firstName: request.FirstName,
                lastName: request.LastName,
                middleName: request.MiddleName,
                dateOfBirth: request.DateOfBirth,
                photoUrl: request.PhotoUrl,
                careerStartYear: request.CareerStartYear,
                specializationId: request.SpecializationId,
                status: request.Status);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                _doctorRepository.Update(updatedDoctor);

                var domainEvent = new DoctorUpdatedEvent
                {
                    OccurredOn = _timeProvider.GetUtcNow().UtcDateTime,
                    DoctorId = updatedDoctor.Id,
                    FirstName = updatedDoctor.FirstName,
                    LastName = updatedDoctor.LastName,
                    MiddleName = updatedDoctor.MiddleName,
                    DateOfBirth = updatedDoctor.DateOfBirth,
                    Email = updatedDoctor.Email,
                    PhotoUrl = updatedDoctor.PhotoUrl,
                    CareerStartYear = updatedDoctor.CareerStartYear,
                    SpecializationId = updatedDoctor.SpecializationId,
                    Status = updatedDoctor.Status
                };

                await _outboxService.AddMessageAsync(domainEvent, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return UnitResult.Success<Exception>();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return UnitResult.Failure(ex);
            }
        }
        catch (Exception ex)
        {
            return UnitResult.Failure(ex);
        }
    }
}
