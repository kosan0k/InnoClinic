using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly WriteDbContext _context;

    public ServiceRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .AnyAsync(s => s.Id == id, cancellationToken);
    }
}

