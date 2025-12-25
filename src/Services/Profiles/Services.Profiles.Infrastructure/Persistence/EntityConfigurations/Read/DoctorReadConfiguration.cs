using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.EntityConfigurations.Read;

public sealed class DoctorReadConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
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

        // Read model optimizations - indexes for common queries
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.LastName, d.FirstName });
    }
}

