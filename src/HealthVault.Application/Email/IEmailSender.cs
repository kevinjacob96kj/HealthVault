namespace HealthVault.Application.Email;

/// <summary>
/// Sends outbound email messages.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toAddress,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default);
}
