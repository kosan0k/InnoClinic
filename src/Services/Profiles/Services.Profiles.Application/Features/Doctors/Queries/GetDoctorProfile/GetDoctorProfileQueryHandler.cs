using CSharpFunctionalExtensions;
using MediatR;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;

public sealed class GetDoctorProfileQueryHandler : IRequestHandler<GetDoctorProfileQuery, Result<DoctorProfileVm, Exception>>
{
    private readonly IDoctorReadRepository _doctorReadRepository;

    public GetDoctorProfileQueryHandler(IDoctorReadRepository doctorReadRepository)
    {
        _doctorReadRepository = doctorReadRepository;
    }

    public async Task<Result<DoctorProfileVm, Exception>> Handle(GetDoctorProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var doctor = await _doctorReadRepository.GetByIdAsync(request.DoctorId, cancellationToken);

            if (doctor is null)
            {
                return Result.Failure<DoctorProfileVm, Exception>(
                    new NotFoundException(nameof(Doctor), request.DoctorId));
            }

            return Result.Success<DoctorProfileVm, Exception>(doctor);
        }
        catch (Exception ex)
        {
            return Result.Failure<DoctorProfileVm, Exception>(ex);
        }
    }
}
