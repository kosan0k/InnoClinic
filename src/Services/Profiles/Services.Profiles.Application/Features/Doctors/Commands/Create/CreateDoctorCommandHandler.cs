using MediatR;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.Create;

public sealed class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Guid>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = Doctor.Create(
            firstName: request.FirstName,
            lastName: request.LastName,
            middleName: request.MiddleName,
            dateOfBirth: request.DateOfBirth,
            email: request.Email,
            photoUrl: request.PhotoUrl,
            careerStartYear: request.CareerStartYear);

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

