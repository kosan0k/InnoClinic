using Microsoft.EntityFrameworkCore;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence;

public sealed class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options)
    {
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReadDbContext).Assembly,
            type => type.Namespace?.Contains("Read") == true);

        base.OnModelCreating(modelBuilder);
    }
}

