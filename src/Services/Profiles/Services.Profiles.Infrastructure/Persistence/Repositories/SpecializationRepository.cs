using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public sealed class SpecializationRepository : ISpecializationRepository
{
    private readonly WriteDbContext _context;

    public SpecializationRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<Specialization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .Include(s => s.Services)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .AnyAsync(s => s.Id == id, cancellationToken);
    }
}
