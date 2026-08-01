namespace HealthVault.Domain.Entities;

/// <summary>
/// A clinical observation / result for a patient, coded with LOINC.
/// </summary>
public class PatientData
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int LoincCodeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Units { get; set; }
    public DateTime ObservedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public LoincCode LoincCode { get; set; } = null!;
}
