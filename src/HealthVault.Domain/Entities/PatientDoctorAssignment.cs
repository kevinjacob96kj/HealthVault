namespace HealthVault.Domain.Entities;

public class PatientDoctorAssignment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int HealthcareStaffId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string? Notes { get; set; }

    public Patient Patient { get; set; } = null!;
    public HealthcareStaff Doctor { get; set; } = null!;
}
