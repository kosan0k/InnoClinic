using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.Profiles.Infrastructure.Persistence.ReadModels;

namespace Services.Profiles.Infrastructure.Persistence.EntityConfigurations.Read;

public sealed class DoctorReadConfiguration : IEntityTypeConfiguration<DoctorReadModel>
{
    public void Configure(EntityTypeBuilder<DoctorReadModel> builder)
    {
        builder.ToTable("Doctors", "read");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.MiddleName)
            .HasMaxLength(100);

        builder.Property(d => d.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(d => d.DateOfBirth)
            .IsRequired();

        builder.Property(d => d.CareerStartYear)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(d => d.SpecializationId)
            .IsRequired();

        builder.Property(d => d.SpecializationName)
            .IsRequired()
            .HasMaxLength(200);

        // Store Services as a PostgreSQL text array for efficient querying
        builder.Property(d => d.Services)
            .HasColumnType("text[]")
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Read model optimizations - indexes for common queries
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.LastName, d.FirstName });
        builder.HasIndex(d => d.SpecializationId);
        builder.HasIndex(d => d.SpecializationName);
    }
}
