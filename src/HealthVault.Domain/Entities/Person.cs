namespace HealthVault.Domain.Entities;

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<UserClaim> Claims { get; set; } = new List<UserClaim>();
}
