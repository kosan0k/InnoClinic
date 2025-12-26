using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;
using Services.Profiles.Domain.Enums;

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

    public async Task CreateAsync(Doctor doctor, CancellationToken ct)
    {
        await _context.Doctors.AddAsync(doctor, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Doctor updatedDoctor, CancellationToken ct)
    {
        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == updatedDoctor.Id, ct);

        if (existing is null)
        {
            await CreateAsync(updatedDoctor, ct); // Fallback to Create
            return;
        }

        _context.Entry(existing).CurrentValues.SetValues(updatedDoctor);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken ct)
    {
        await _context.Doctors
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(calls => calls.SetProperty(d => d.Status, status), ct);
    }
}
