namespace HealthVault.Application.Email;

/// <summary>
/// Configurable email delivery settings for OTP and other messages.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Email provider to use: <c>Smtp</c> or <c>Console</c>.
    /// </summary>
    public string Provider { get; set; } = "Console";

    public string FromAddress { get; set; } = "noreply@healthvault.local";

    public string FromName { get; set; } = "HealthVault";

    /// <summary>
    /// When true, OTP codes are also returned in the API response for local testing.
    /// Keep this false when real email delivery is enabled.
    /// </summary>
    public bool IncludeOtpInApiResponse { get; set; }

    /// <summary>
    /// Optional test override. When set, all OTP emails are delivered here
    /// instead of the patient's registered email.
    /// </summary>
    public string? OverrideToAddress { get; set; }

    public SmtpOptions Smtp { get; set; } = new();
}

/// <summary>
/// SMTP connection settings used when <see cref="EmailOptions.Provider"/> is <c>Smtp</c>.
/// </summary>
public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
