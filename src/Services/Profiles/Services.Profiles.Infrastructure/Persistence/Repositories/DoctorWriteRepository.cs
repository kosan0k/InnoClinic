using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public sealed class DoctorWriteRepository : IDoctorWriteRepository
{
    private readonly WriteDbContext _context;

    public DoctorWriteRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Note: IsDeleted filter is applied automatically via global query filter
        // AsNoTracking is used because Doctor is an immutable record - updates create new instances
        // which would conflict with tracked entities when calling Update()
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        await _context.Doctors.AddAsync(doctor, cancellationToken);
    }

    public void Update(Doctor doctor)
    {
        _context.Doctors.Update(doctor);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

