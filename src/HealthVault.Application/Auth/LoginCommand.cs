using FluentValidation;
using HealthVault.Application.People;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Auth;

/// <summary>
/// Authenticated user returned by a successful login.
/// </summary>
public record LoginUserModel(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    bool MustChangePassword,
    IReadOnlyList<string> Roles);

/// <summary>
/// Command that authenticates a person by email and password.
/// </summary>
public record LoginCommand : IRequest<LoginUserModel>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// "staff" or "patient" — limits which roles may sign in on each form.
    /// </summary>
    public string Mode { get; init; } = "staff";
}

/// <summary>
/// Validates a <see cref="LoginCommand"/>.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Mode)
            .Must(mode => mode is "staff" or "patient")
            .WithMessage("Mode must be staff or patient.");
    }
}

/// <summary>
/// Handles a <see cref="LoginCommand"/>.
/// </summary>
public class LoginHandler : IRequestHandler<LoginCommand, LoginUserModel>
{
    private static readonly HashSet<string> StaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "CentralAdmin",
        "Doctor",
        "Nurse",
        "Staff"
    };

    private readonly AppDbContext _context;

    public LoginHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoginUserModel> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var password = request.Password.Trim();
        var mode = request.Mode.Trim().ToLowerInvariant();

        var person = await _context.People
            .AsNoTracking()
            .Include(item => item.Claims)
            .SingleOrDefaultAsync(
                item => item.Email == email,
                cancellationToken);

        if (person is null ||
            !string.Equals(person.Password, password, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = person.Claims
            .Select(claim => claim.Role)
            .OrderBy(role => role)
            .ToList();

        var hasStaffRole = roles.Any(role => StaffRoles.Contains(role));
        var hasPatientRole = roles.Any(role => PatientAuthHelper.PatientRole.Contains(role));

        if (mode == "patient")
        {
            if (!hasPatientRole)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var patientIsActive = await _context.Patients
                .AsNoTracking()
                .Where(patient => patient.PersonId == person.Id)
                .Select(patient => (bool?)patient.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (patientIsActive == false)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }
        }
        else if (!hasStaffRole)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        else
        {
            var staffIsActive = await _context.HealthcareStaff
                .AsNoTracking()
                .Where(staff => staff.PersonId == person.Id)
                .Select(staff => (bool?)staff.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (staffIsActive == false)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }
        }

        return new LoginUserModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            person.MustChangePassword,
            roles);
    }
}
