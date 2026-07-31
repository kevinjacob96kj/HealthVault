using FluentValidation;
using FluentValidation.Results;
using HealthVault.Application.People;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Auth;

public record PatientSignupVerifiedModel(string SignupToken, string Message);

public record VerifyPatientSignupCommand : IRequest<PatientSignupVerifiedModel>
{
    public string AbhaId { get; init; } = string.Empty;
    public string AadhaarNumber { get; init; } = string.Empty;
    public string DateOfBirth { get; init; } = string.Empty;
}

public class VerifyPatientSignupCommandValidator : AbstractValidator<VerifyPatientSignupCommand>
{
    public VerifyPatientSignupCommandValidator()
    {
        RuleFor(command => command.AbhaId)
            .NotEmpty()
            .MaximumLength(20)
            .Must(value => new string(value.Where(char.IsDigit).ToArray()).Length == 14)
            .WithMessage("ABHA ID must contain 14 digits.");

        RuleFor(command => command.AadhaarNumber)
            .NotEmpty()
            .Must(value => PatientSignupSessionStore.NormalizeAadhaar(value).Length == 12)
            .WithMessage("Aadhaar number must be 12 digits.");

        RuleFor(command => command.DateOfBirth)
            .NotEmpty()
            .Must(value => DateOnly.TryParse(value, out var dob) &&
                           dob <= DateOnly.FromDateTime(DateTime.UtcNow.Date) &&
                           dob >= DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-120)))
            .WithMessage("Enter a valid date of birth.");
    }
}

public class VerifyPatientSignupHandler
    : IRequestHandler<VerifyPatientSignupCommand, PatientSignupVerifiedModel>
{
    private readonly AppDbContext _context;
    private readonly PatientSignupSessionStore _sessions;

    public VerifyPatientSignupHandler(AppDbContext context, PatientSignupSessionStore sessions)
    {
        _context = context;
        _sessions = sessions;
    }

    public async Task<PatientSignupVerifiedModel> Handle(
        VerifyPatientSignupCommand request,
        CancellationToken cancellationToken)
    {
        var abhaId = PatientAuthHelper.NormalizeAbhaId(request.AbhaId);
        var aadhaar = PatientSignupSessionStore.NormalizeAadhaar(request.AadhaarNumber);
        if (!DateOnly.TryParse(request.DateOfBirth, out var dateOfBirth))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(VerifyPatientSignupCommand.DateOfBirth),
                    "Enter a valid date of birth.")
            ]);
        }

        var existingPatient = await _context.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(patient => patient.AbhaId == abhaId, cancellationToken);

        if (existingPatient is not null)
        {
            if (existingPatient.DateOfBirth != dateOfBirth)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(VerifyPatientSignupCommand.DateOfBirth),
                        "Date of birth does not match the ABHA ID on file.")
                ]);
            }

            if (!string.IsNullOrWhiteSpace(existingPatient.AadhaarNumber) &&
                !string.Equals(existingPatient.AadhaarNumber, aadhaar, StringComparison.Ordinal))
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(VerifyPatientSignupCommand.AadhaarNumber),
                        "Aadhaar number does not match the ABHA ID on file.")
                ]);
            }

            var alreadyRegistered = await _context.People
                .AsNoTracking()
                .Include(person => person.Claims)
                .AnyAsync(
                    person =>
                        person.Id == existingPatient.PersonId &&
                        person.Claims.Any(claim => PatientAuthHelper.PatientRole.Contains(claim.Role)),
                    cancellationToken);

            if (alreadyRegistered)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(VerifyPatientSignupCommand.AbhaId),
                        "An account already exists for this ABHA ID. Please sign in.")
                ]);
            }
        }

        var aadhaarTaken = await _context.Patients
            .AsNoTracking()
            .AnyAsync(
                patient =>
                    patient.AadhaarNumber == aadhaar &&
                    patient.AbhaId != abhaId,
                cancellationToken);

        if (aadhaarTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(VerifyPatientSignupCommand.AadhaarNumber),
                    "This Aadhaar number is already linked to another patient.")
            ]);
        }

        var token = _sessions.Create(abhaId, aadhaar, dateOfBirth);
        return new PatientSignupVerifiedModel(
            token,
            "Identity verified. Continue to create your login.");
    }
}

public record CompletePatientSignupCommand : IRequest<LoginUserModel>
{
    public string SignupToken { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Gender { get; init; } = "Man";
    public string MobileNumber { get; init; } = string.Empty;
    public bool UsedGoogle { get; init; }
}

public class CompletePatientSignupCommandValidator : AbstractValidator<CompletePatientSignupCommand>
{
    public CompletePatientSignupCommandValidator()
    {
        RuleFor(command => command.SignupToken).NotEmpty();
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Gender)
            .Must(value => value is "Man" or "Women" or "Transgender")
            .WithMessage("Gender must be Man, Women, or Transgender.");
        RuleFor(command => command.MobileNumber).MaximumLength(20);

        When(command => !command.UsedGoogle, () =>
        {
            RuleFor(command => command.Password!).StrongPassword();
        });
    }
}

public class CompletePatientSignupHandler
    : IRequestHandler<CompletePatientSignupCommand, LoginUserModel>
{
    private readonly AppDbContext _context;
    private readonly PatientSignupSessionStore _sessions;

    public CompletePatientSignupHandler(AppDbContext context, PatientSignupSessionStore sessions)
    {
        _context = context;
        _sessions = sessions;
    }

    public async Task<LoginUserModel> Handle(
        CompletePatientSignupCommand request,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryTake(
                request.SignupToken,
                out var abhaId,
                out var aadhaar,
                out var dateOfBirth))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CompletePatientSignupCommand.SignupToken),
                    "Your signup session expired. Start again from ABHA verification.")
            ]);
        }

        var email = request.Email.Trim();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var gender = string.IsNullOrWhiteSpace(request.Gender) ? "Man" : request.Gender.Trim();
        var mobile = string.IsNullOrWhiteSpace(request.MobileNumber)
            ? "—"
            : request.MobileNumber.Trim();

        var emailTaken = await _context.People
            .AsNoTracking()
            .AnyAsync(person => person.Email == email, cancellationToken);

        if (emailTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CompletePatientSignupCommand.Email),
                    "An account already exists with this username/email.")
            ]);
        }

        var patientEmailTaken = await _context.Patients
            .AsNoTracking()
            .AnyAsync(
                patient => patient.Person.Email == email && patient.AbhaId != abhaId,
                cancellationToken);

        if (patientEmailTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CompletePatientSignupCommand.Email),
                    "This email is already used by another patient record.")
            ]);
        }

        var patient = await _context.Patients
            .SingleOrDefaultAsync(item => item.AbhaId == abhaId, cancellationToken);

        var password = request.UsedGoogle
            ? $"Google#{Guid.NewGuid():N}"
            : request.Password!.Trim();

        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password,
            MustChangePassword = false,
            Gender = gender,
            CreatedAt = DateTime.UtcNow,
            Claims =
            [
                new UserClaim { Role = "Patient" }
            ]
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync(cancellationToken);

        if (patient is null)
        {
            patient = new Patient
            {
                PersonId = person.Id,
                AbhaId = abhaId,
                AadhaarNumber = aadhaar,
                DateOfBirth = dateOfBirth,
                MobileNumber = mobile,
                IsActive = true
            };
            _context.Patients.Add(patient);
        }
        else
        {
            patient.PersonId = person.Id;
            patient.AadhaarNumber = aadhaar;
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                patient.MobileNumber = mobile;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginUserModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            MustChangePassword: false,
            ["Patient"]);
    }
}
