using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.SoftDelete;

public sealed class SoftDeleteDoctorCommandHandler : IRequestHandler<SoftDeleteDoctorCommand>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public SoftDeleteDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SoftDeleteDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Doctor), request.DoctorId);

        var deletedDoctor = doctor.MarkAsDeleted();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _doctorRepository.Update(deletedDoctor);

            var domainEvent = new DoctorDeletedEvent
            {
                DoctorId = deletedDoctor.Id
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

