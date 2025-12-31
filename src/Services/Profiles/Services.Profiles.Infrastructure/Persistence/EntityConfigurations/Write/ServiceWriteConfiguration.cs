using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.EntityConfigurations.Write;

public sealed class ServiceWriteConfiguration : IEntityTypeConfiguration<Service>
{
    // Well-known seed data IDs for services
    public static readonly Guid AnalysesId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ConsultationId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DiagnosticsId = new("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", "write");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(s => s.Name)
            .IsUnique();

        // Seed initial services data
        builder.HasData(
            new { Id = AnalysesId, Name = "Analyses", IsActive = true },
            new { Id = ConsultationId, Name = "Consultation", IsActive = true },
            new { Id = DiagnosticsId, Name = "Diagnostics", IsActive = true }
        );
    }
}

