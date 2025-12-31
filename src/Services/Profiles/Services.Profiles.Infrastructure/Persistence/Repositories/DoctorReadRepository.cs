using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;
using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public sealed class DoctorReadRepository : IDoctorReadRepository
{
    private readonly ReadDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DoctorReadRepository(ReadDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<DoctorProfileVm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentYear = _timeProvider.GetUtcNow().Year;
        
        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doctor is null)
        {
            return null;
        }

        return new DoctorProfileVm
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            MiddleName = doctor.MiddleName,
            DateOfBirth = doctor.DateOfBirth,
            Email = doctor.Email,
            PhotoUrl = doctor.PhotoUrl,
            CareerStartYear = doctor.CareerStartYear,
            Experience = currentYear - doctor.CareerStartYear,
            Status = doctor.Status,
            SpecializationId = doctor.SpecializationId,
            SpecializationName = doctor.SpecializationName,
            Services = doctor.Services
        };
    }

    public async Task<IReadOnlyList<DoctorListItemVm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = _timeProvider.GetUtcNow().Year;
        
        var doctors = await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);

        return doctors.Select(d => new DoctorListItemVm
        {
            Id = d.Id,
            FirstName = d.FirstName,
            LastName = d.LastName,
            MiddleName = d.MiddleName,
            PhotoUrl = d.PhotoUrl,
            Experience = currentYear - d.CareerStartYear,
            Status = d.Status,
            SpecializationName = d.SpecializationName,
            Services = d.Services
        }).ToList();
    }

    public async Task<IReadOnlyList<DoctorListItemVm>> GetByStatusAsync(int status, CancellationToken cancellationToken = default)
    {
        var currentYear = _timeProvider.GetUtcNow().Year;
        
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Where(d => (int)d.Status == status)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);

        return doctors.Select(d => new DoctorListItemVm
        {
            Id = d.Id,
            FirstName = d.FirstName,
            LastName = d.LastName,
            MiddleName = d.MiddleName,
            PhotoUrl = d.PhotoUrl,
            Experience = currentYear - d.CareerStartYear,
            Status = d.Status,
            SpecializationName = d.SpecializationName,
            Services = d.Services
        }).ToList();
    }
}
