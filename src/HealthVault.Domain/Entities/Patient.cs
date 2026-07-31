namespace HealthVault.Domain.Entities;

public class Patient
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string AbhaId { get; set; } = string.Empty;
    public string? AadhaarNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Person Person { get; set; } = null!;
    public ICollection<PatientDoctorAssignment> DoctorAssignments { get; set; } =
        new List<PatientDoctorAssignment>();
}
