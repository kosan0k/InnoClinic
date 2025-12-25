using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.ChangeStatus;

public sealed class ChangeDoctorStatusCommandHandler : IRequestHandler<ChangeDoctorStatusCommand>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDoctorStatusCommandHandler(
        IDoctorWriteRepository doctorRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeDoctorStatusCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Doctor), request.DoctorId);

        var oldStatus = doctor.Status;
        var updatedDoctor = doctor.WithStatus(request.NewStatus);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _doctorRepository.Update(updatedDoctor);

            var domainEvent = new DoctorStatusChangedEvent
            {
                DoctorId = updatedDoctor.Id,
                OldStatus = oldStatus,
                NewStatus = request.NewStatus
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

