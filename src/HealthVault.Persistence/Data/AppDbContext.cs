using HealthVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Persistence.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> People => Set<Person>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<ApiRequest> Requests => Set<ApiRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("People", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Password).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MustChangePassword).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.ToTable("UserClaims", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => new { e.PersonId, e.Role }).IsUnique();
            entity.HasOne(e => e.Person)
                .WithMany(p => p.Claims)
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiRequest>(entity =>
        {
            entity.ToTable("Requests", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestApi).HasMaxLength(512).IsRequired();
            entity.Property(e => e.RequestDateTime)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
            entity.Property(e => e.WasSuccessful).IsRequired();
            entity.Property(e => e.RequestedBy).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.RequestDateTime);
        });
    }
}
