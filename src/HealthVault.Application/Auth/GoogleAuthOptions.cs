namespace HealthVault.Application.Auth;

public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// When true and ClientId is empty, demo Google email signup is allowed (local/dev).
    /// </summary>
    public bool AllowDemoSignIn { get; set; } = true;
}

public record GoogleAuthConfigModel(string? ClientId, bool AllowDemoSignIn);
