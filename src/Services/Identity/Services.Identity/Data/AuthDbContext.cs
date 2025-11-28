using Microsoft.EntityFrameworkCore;
using Services.Identity.Entities;

namespace Services.Identity.Data;

/// <summary>
/// Entity Framework Core database context for the Auth Service.
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Local user entities synchronized from Keycloak.
    /// </summary>
    public DbSet<LocalUser> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalUser>(entity =>
        {
            entity.ToTable("local_users");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.KeycloakUserId)
                .HasColumnName("keycloak_user_id")
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Username)
                .HasColumnName("username")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastSyncedAt)
                .HasColumnName("last_synced_at");

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.KeycloakUserId)
                .IsUnique()
                .HasDatabaseName("ix_local_users_keycloak_user_id");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("ix_local_users_email");

            entity.HasIndex(e => e.Username)
                .HasDatabaseName("ix_local_users_username");
        });
    }
}

