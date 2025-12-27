using Microsoft.EntityFrameworkCore;
using Services.Profiles.Infrastructure.Persistence.ReadModels;

namespace Services.Profiles.Infrastructure.Persistence;

public sealed class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options)
    {
    }

    public DbSet<DoctorReadModel> Doctors => Set<DoctorReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReadDbContext).Assembly,
            type => type.Namespace?.Contains("Read") == true);

        base.OnModelCreating(modelBuilder);
    }
}
