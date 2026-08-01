namespace HealthVault.Domain.Entities;

public class ApiRequest
{
    public long Id { get; set; }
    public string RequestApi { get; set; } = string.Empty;
    public DateTime RequestDateTime { get; set; }
    public bool WasSuccessful { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}
