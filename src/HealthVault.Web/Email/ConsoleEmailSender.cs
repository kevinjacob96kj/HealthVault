using HealthVault.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthVault.Web.Email;

/// <summary>
/// Logs email content instead of sending it. Useful for local development.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(
        IOptions<EmailOptions> options,
        ILogger<ConsoleEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendAsync(
        string toAddress,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            [Console email provider]
            From: {FromName} <{FromAddress}>
            To: {ToAddress}
            Subject: {Subject}
            Body:
            {Body}
            """,
            _options.FromName,
            _options.FromAddress,
            toAddress,
            subject,
            plainTextBody);

        return Task.CompletedTask;
    }
}
