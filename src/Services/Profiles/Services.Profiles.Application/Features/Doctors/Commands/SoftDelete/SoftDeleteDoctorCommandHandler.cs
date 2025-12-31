using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Interfaces;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Events;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Commands.SoftDelete;

public sealed class SoftDeleteDoctorCommandHandler : IRequestHandler<SoftDeleteDoctorCommand, UnitResult<Exception>>
{
    private readonly IDoctorWriteRepository _doctorRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SoftDeleteDoctorCommandHandler(
        IDoctorWriteRepository doctorRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _doctorRepository = doctorRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<UnitResult<Exception>> Handle(SoftDeleteDoctorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken);
            
            if (doctor is null)
            {
                return new NotFoundException(nameof(Doctor), request.DoctorId);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Use Remove - the SoftDeleteInterceptor will convert this to a soft delete
                _doctorRepository.Remove(doctor);

                var domainEvent = new DoctorDeletedEvent
                {
                    OccurredOn = _timeProvider.GetUtcNow().UtcDateTime,
                    DoctorId = doctor.Id
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
