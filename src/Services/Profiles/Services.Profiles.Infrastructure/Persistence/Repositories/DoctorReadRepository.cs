using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public sealed class DoctorReadRepository : IDoctorReadRepository
{
    private readonly ReadDbContext _context;

    public DoctorReadRepository(ReadDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Doctor>> GetByStatusAsync(int status, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => (int)d.Status == status)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);
    }
}

