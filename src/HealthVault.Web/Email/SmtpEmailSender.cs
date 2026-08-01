using System.Net;
using System.Net.Mail;
using HealthVault.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthVault.Web.Email;

/// <summary>
/// Sends email through a configured SMTP server.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toAddress,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default)
    {
        var smtp = _options.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException(
                "Email:Smtp:Host is required when Email:Provider is set to Smtp.");
        }

        if (string.IsNullOrWhiteSpace(smtp.UserName) || string.IsNullOrWhiteSpace(smtp.Password))
        {
            throw new InvalidOperationException(
                "Gmail SMTP requires Email:Smtp:UserName and Email:Smtp:Password. " +
                "Set them with user secrets, for example: " +
                "dotnet user-secrets set \"Email:Smtp:Password\" \"YOUR_APP_PASSWORD\" --project src/HealthVault.Web");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = plainTextBody,
            IsBodyHtml = false
        };
        message.To.Add(toAddress);

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtp.UserName, smtp.Password)
        };

        _logger.LogInformation(
            "Sending email to {ToAddress} via SMTP {Host}:{Port}",
            toAddress,
            smtp.Host,
            smtp.Port);

        await client.SendMailAsync(message, cancellationToken);
    }
}
