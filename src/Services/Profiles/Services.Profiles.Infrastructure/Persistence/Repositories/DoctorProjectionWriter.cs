using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Enums;
using Services.Profiles.Infrastructure.Persistence.ReadModels;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public class DoctorProjectionWriter : IDoctorProjectionWriter
{
    private readonly ReadDbContext _context;

    public DoctorProjectionWriter(ReadDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return await _context.Doctors.AnyAsync(d => d.Id == id, ct);
    }

    public async Task CreateAsync(DoctorProjectionData data, CancellationToken cancellationToken)
    {
        var readModel = new DoctorReadModel
        {
            Id = data.Id,
            FirstName = data.FirstName,
            LastName = data.LastName,
            MiddleName = data.MiddleName,
            DateOfBirth = data.DateOfBirth,
            Email = data.Email,
            PhotoUrl = data.PhotoUrl,
            CareerStartYear = data.CareerStartYear,
            Status = data.Status,
            SpecializationId = data.SpecializationId,
            SpecializationName = data.SpecializationName,
            Services = data.Services
        };

        await _context.Doctors.AddAsync(readModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DoctorProjectionData data, CancellationToken cancellationToken)
    {
        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == data.Id, cancellationToken);

        if (existing is null)
        {
            // Fallback to Create if entity doesn't exist
            await CreateAsync(data, cancellationToken);
            return;
        }

        existing.FirstName = data.FirstName;
        existing.LastName = data.LastName;
        existing.MiddleName = data.MiddleName;
        existing.DateOfBirth = data.DateOfBirth;
        existing.Email = data.Email;
        existing.PhotoUrl = data.PhotoUrl;
        existing.CareerStartYear = data.CareerStartYear;
        existing.Status = data.Status;
        existing.SpecializationId = data.SpecializationId;
        existing.SpecializationName = data.SpecializationName;
        existing.Services = data.Services;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken ct)
    {
        await _context.Doctors
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(calls => calls.SetProperty(d => d.Status, status), ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _context.Doctors
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
