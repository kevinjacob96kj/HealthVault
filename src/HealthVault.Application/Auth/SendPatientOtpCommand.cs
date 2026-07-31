using FluentValidation;
using HealthVault.Application.Email;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthVault.Application.Auth;

/// <summary>
/// Result returned after requesting a patient login OTP.
/// </summary>
public record PatientOtpSentModel(
    string Message,
    string Destination,
    int ExpiresInSeconds,
    string? Otp);

/// <summary>
/// Command that sends a one-time password for patient login.
/// </summary>
public record SendPatientOtpCommand : IRequest<PatientOtpSentModel>
{
    public string AbhaId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Validates a <see cref="SendPatientOtpCommand"/>.
/// </summary>
public class SendPatientOtpCommandValidator : AbstractValidator<SendPatientOtpCommand>
{
    public SendPatientOtpCommandValidator()
    {
        RuleFor(command => command.AbhaId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

/// <summary>
/// Handles a <see cref="SendPatientOtpCommand"/>.
/// </summary>
public class SendPatientOtpHandler : IRequestHandler<SendPatientOtpCommand, PatientOtpSentModel>
{
    private readonly AppDbContext _context;
    private readonly PatientOtpStore _otpStore;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<SendPatientOtpHandler> _logger;

    public SendPatientOtpHandler(
        AppDbContext context,
        PatientOtpStore otpStore,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<SendPatientOtpHandler> logger)
    {
        _context = context;
        _otpStore = otpStore;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<PatientOtpSentModel> Handle(
        SendPatientOtpCommand request,
        CancellationToken cancellationToken)
    {
        var abhaId = PatientAuthHelper.NormalizeAbhaId(request.AbhaId);
        var email = request.Email.Trim();

        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(
                item => item.AbhaId == abhaId && item.Person.Email == email,
                cancellationToken);

        if (!patientExists)
        {
            throw new UnauthorizedAccessException("Invalid ABHA ID or email.");
        }

        var person = await _context.People
            .AsNoTracking()
            .Include(item => item.Claims)
            .SingleOrDefaultAsync(
                item => item.Email == email,
                cancellationToken);

        if (person is null ||
            !person.Claims.Any(claim => PatientAuthHelper.PatientRole.Contains(claim.Role)))
        {
            throw new UnauthorizedAccessException("Invalid ABHA ID or email.");
        }

        var otp = _otpStore.Create(abhaId, email);
        var deliveryAddress = string.IsNullOrWhiteSpace(_emailOptions.OverrideToAddress)
            ? email
            : _emailOptions.OverrideToAddress.Trim();
        var destination = PatientAuthHelper.MaskEmail(deliveryAddress);

        var body = $"""
            Your HealthVault patient login code is {otp}.

            This code expires in 5 minutes.
            Requested for patient account: {email}
            If you did not request this code, you can ignore this email.
            """;

        await _emailSender.SendAsync(
            deliveryAddress,
            "Your HealthVault login code",
            body,
            cancellationToken);

        _logger.LogInformation(
            "Patient login OTP for account {AccountEmail} delivered to {DeliveryEmail} via {Provider}",
            email,
            deliveryAddress,
            _emailOptions.Provider);

        return new PatientOtpSentModel(
            $"OTP sent to {destination}.",
            destination,
            300,
            _emailOptions.IncludeOtpInApiResponse ? otp : null);
    }
}
