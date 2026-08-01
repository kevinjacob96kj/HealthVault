namespace HealthVault.Domain.Entities;

public class UserClaim
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string Role { get; set; } = string.Empty;

    public Person Person { get; set; } = null!;
}
