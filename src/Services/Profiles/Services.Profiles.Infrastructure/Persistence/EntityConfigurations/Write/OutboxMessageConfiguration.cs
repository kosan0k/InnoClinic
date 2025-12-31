using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.Profiles.Domain.Entities;

namespace Services.Profiles.Infrastructure.Persistence.EntityConfigurations.Write;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "write");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.OccurredOn)
            .IsRequired();

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        // Use quoted column names for PostgreSQL compatibility
        builder.HasIndex(x => new { x.ProcessedOn, x.OccurredOn })
           .HasFilter("\"ProcessedOn\" IS NULL");
    }
}
