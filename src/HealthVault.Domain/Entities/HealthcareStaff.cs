namespace HealthVault.Domain.Entities;

public class HealthcareStaff
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public int HealthcareProviderId { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Person Person { get; set; } = null!;
    public HealthcareProvider HealthcareProvider { get; set; } = null!;
    public ICollection<PatientDoctorAssignment> PatientAssignments { get; set; } =
        new List<PatientDoctorAssignment>();
}
