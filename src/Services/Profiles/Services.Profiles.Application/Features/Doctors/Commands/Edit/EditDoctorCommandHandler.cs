using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.Edit;

public sealed class EditDoctorCommandHandler : IRequestHandler<EditDoctorCommand>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public EditDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EditDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Doctor), request.Id);

        var updatedDoctor = doctor.Update(
            firstName: request.FirstName,
            lastName: request.LastName,
            middleName: request.MiddleName,
            dateOfBirth: request.DateOfBirth,
            photoUrl: request.PhotoUrl,
            careerStartYear: request.CareerStartYear,
            status: request.Status);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _doctorRepository.Update(updatedDoctor);

            var domainEvent = new DoctorUpdatedEvent
            {
                DoctorId = updatedDoctor.Id,
                FirstName = updatedDoctor.FirstName,
                LastName = updatedDoctor.LastName,
                MiddleName = updatedDoctor.MiddleName,
                DateOfBirth = updatedDoctor.DateOfBirth,
                Email = updatedDoctor.Email,
                PhotoUrl = updatedDoctor.PhotoUrl,
                CareerStartYear = updatedDoctor.CareerStartYear,
                Status = updatedDoctor.Status
            };

            await _outboxService.AddMessageAsync(domainEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

