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
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientDoctorAssignment> PatientDoctorAssignments => Set<PatientDoctorAssignment>();
    public DbSet<HealthcareProvider> HealthcareProviders => Set<HealthcareProvider>();
    public DbSet<HealthcareStaff> HealthcareStaff => Set<HealthcareStaff>();
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
            entity.Property(e => e.MustChangePassword).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(20).IsRequired();
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

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AbhaId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.AadhaarNumber).HasMaxLength(12);
            entity.Property(e => e.DateOfBirth).IsRequired();
            entity.Property(e => e.MobileNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => e.PersonId).IsUnique();
            entity.HasIndex(e => e.AbhaId).IsUnique();
            entity.HasIndex(e => e.AadhaarNumber)
                .IsUnique()
                .HasFilter("[AadhaarNumber] IS NOT NULL");
            entity.HasOne(e => e.Person)
                .WithOne(person => person.PatientProfile)
                .HasForeignKey<Patient>(e => e.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PatientDoctorAssignment>(entity =>
        {
            entity.ToTable("PatientDoctorAssignments", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.HealthcareStaffId);
            entity.HasIndex(e => new { e.PatientId, e.HealthcareStaffId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasOne(e => e.Patient)
                .WithMany(patient => patient.DoctorAssignments)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Doctor)
                .WithMany(staff => staff.PatientAssignments)
                .HasForeignKey(e => e.HealthcareStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HealthcareProvider>(entity =>
        {
            entity.ToTable("Provider", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProviderType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(256).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.State).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => e.ProviderCode).IsUnique();
        });

        modelBuilder.Entity<HealthcareStaff>(entity =>
        {
            entity.ToTable("Staff", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StaffCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Specialty).HasMaxLength(100);
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => e.StaffCode).IsUnique();
            entity.HasIndex(e => e.PersonId).IsUnique();
            entity.HasIndex(e => e.HealthcareProviderId);
            entity.HasOne(e => e.HealthcareProvider)
                .WithMany(provider => provider.StaffMembers)
                .HasForeignKey(e => e.HealthcareProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Person)
                .WithOne(person => person.StaffAssignment)
                .HasForeignKey<HealthcareStaff>(e => e.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
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
