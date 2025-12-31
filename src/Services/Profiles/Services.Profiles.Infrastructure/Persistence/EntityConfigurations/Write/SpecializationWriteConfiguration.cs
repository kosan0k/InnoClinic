using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.EntityConfigurations.Write;

public sealed class SpecializationWriteConfiguration : IEntityTypeConfiguration<Specialization>
{
    // Well-known seed data IDs for specializations
    public static readonly Guid TherapistId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public static readonly Guid SurgeonId = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    public static readonly Guid CardiologistId = new("c3d4e5f6-a7b8-9012-cdef-123456789012");

    public void Configure(EntityTypeBuilder<Specialization> builder)
    {
        builder.ToTable("Specializations", "write");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure the Services navigation to use the backing field
        builder.Navigation(s => s.Services)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure many-to-many relationship with Service via join table
        builder.HasMany(s => s.Services)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "SpecializationServices",
                j => j
                    .HasOne<Service>()
                    .WithMany()
                    .HasForeignKey("ServiceId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<Specialization>()
                    .WithMany()
                    .HasForeignKey("SpecializationId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("SpecializationServices", "write");
                    j.HasKey("SpecializationId", "ServiceId");

                    // Seed the many-to-many relationships
                    j.HasData(
                        // Therapist: Consultation, Diagnostics
                        new { SpecializationId = TherapistId, ServiceId = ServiceWriteConfiguration.ConsultationId },
                        new { SpecializationId = TherapistId, ServiceId = ServiceWriteConfiguration.DiagnosticsId },
                        // Surgeon: Analyses, Diagnostics
                        new { SpecializationId = SurgeonId, ServiceId = ServiceWriteConfiguration.AnalysesId },
                        new { SpecializationId = SurgeonId, ServiceId = ServiceWriteConfiguration.DiagnosticsId },
                        // Cardiologist: Consultation, Diagnostics, Analyses
                        new { SpecializationId = CardiologistId, ServiceId = ServiceWriteConfiguration.ConsultationId },
                        new { SpecializationId = CardiologistId, ServiceId = ServiceWriteConfiguration.DiagnosticsId },
                        new { SpecializationId = CardiologistId, ServiceId = ServiceWriteConfiguration.AnalysesId }
                    );
                });

        builder.HasIndex(s => s.Name)
            .IsUnique();

        // Seed specializations data
        builder.HasData(
            new { Id = TherapistId, Name = "Therapist", IsActive = true },
            new { Id = SurgeonId, Name = "Surgeon", IsActive = true },
            new { Id = CardiologistId, Name = "Cardiologist", IsActive = true }
        );
    }
}
