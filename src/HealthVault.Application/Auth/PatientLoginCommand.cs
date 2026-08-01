using FluentValidation;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Auth;

/// <summary>
/// Command that authenticates a patient by ABHA ID, email, and OTP.
/// </summary>
public record PatientLoginCommand : IRequest<LoginUserModel>
{
    public string AbhaId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}

/// <summary>
/// Validates a <see cref="PatientLoginCommand"/>.
/// </summary>
public class PatientLoginCommandValidator : AbstractValidator<PatientLoginCommand>
{
    public PatientLoginCommandValidator()
    {
        RuleFor(command => command.AbhaId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Otp)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$")
            .WithMessage("OTP must be a 6-digit code.");
    }
}

/// <summary>
/// Handles a <see cref="PatientLoginCommand"/>.
/// </summary>
public class PatientLoginHandler : IRequestHandler<PatientLoginCommand, LoginUserModel>
{
    private readonly AppDbContext _context;
    private readonly PatientOtpStore _otpStore;

    public PatientLoginHandler(AppDbContext context, PatientOtpStore otpStore)
    {
        _context = context;
        _otpStore = otpStore;
    }

    public async Task<LoginUserModel> Handle(
        PatientLoginCommand request,
        CancellationToken cancellationToken)
    {
        var abhaId = PatientAuthHelper.NormalizeAbhaId(request.AbhaId);
        var email = request.Email.Trim();

        if (!_otpStore.TryValidate(abhaId, email, request.Otp))
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(
                item => item.AbhaId == abhaId && item.Person.Email == email && item.IsActive,
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

        return new LoginUserModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            MustChangePassword: false,
            person.Claims
                .Select(claim => claim.Role)
                .OrderBy(role => role)
                .ToList());
    }
}
