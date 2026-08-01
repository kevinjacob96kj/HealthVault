namespace HealthVault.Domain.Entities;

/// <summary>
/// LOINC reference code used to identify clinical observations and lab tests.
/// </summary>
public class LoincCode
{
    public int Id { get; set; }
    public string LoincNum { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string? Property { get; set; }
    public string? TimeAspct { get; set; }
    public string? System { get; set; }
    public string? ScaleTyp { get; set; }
    public string? MethodTyp { get; set; }
    public string? Class { get; set; }
    public string? ShortName { get; set; }
    public string LongCommonName { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public byte? ClassType { get; set; }
    public string? ExampleUnits { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<PatientData> PatientDataRows { get; set; } = new List<PatientData>();
}
