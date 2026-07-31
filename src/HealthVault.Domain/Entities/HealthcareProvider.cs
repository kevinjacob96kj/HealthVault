namespace HealthVault.Domain.Entities;

public class HealthcareProvider
{
    public int Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<HealthcareStaff> StaffMembers { get; set; } = new List<HealthcareStaff>();
}
