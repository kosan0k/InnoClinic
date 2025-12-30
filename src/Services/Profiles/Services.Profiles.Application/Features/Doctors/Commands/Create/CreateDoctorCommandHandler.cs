using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.Create;

public sealed class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Result<Guid, Exception>>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly ISpecializationRepository _specializationRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateDoctorCommandHandler(
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

    public async Task<Result<Guid, Exception>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that the specialization exists
            var specializationExists = await _specializationRepository.ExistsAsync(
                request.SpecializationId, 
                cancellationToken);

            if (!specializationExists)
            {
                return Result.Failure<Guid, Exception>(
                    new NotFoundException(nameof(Specialization), request.SpecializationId.ToString()));
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
                    OccurredOn = _timeProvider.GetUtcNow().UtcDateTime,
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

                return Result.Success<Guid, Exception>(doctor.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<Guid, Exception>(ex);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid, Exception>(ex);
        }
    }
}
