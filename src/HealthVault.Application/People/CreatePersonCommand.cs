using FluentValidation;
using FluentValidation.Results;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that creates a person at the signed-in admin's hospital.
/// </summary>
public record CreatePersonCommand : IRequest<PersonModel>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string RequestedByEmail { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>
/// Validates a <see cref="CreatePersonCommand"/>.
/// </summary>
public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.RequestedByEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Roles)
            .NotEmpty()
            .WithMessage("At least one role is required.")
            .Must(PeopleRoles.AreHospitalAssignable)
            .WithMessage($"Roles must be one of: {string.Join(", ", PeopleRoles.HospitalAssignable)}.");
    }
}

/// <summary>
/// Handles a <see cref="CreatePersonCommand"/>.
/// </summary>
public class CreatePersonHandler : IRequestHandler<CreatePersonCommand, PersonModel>
{
    private readonly AppDbContext _context;

    public CreatePersonHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PersonModel> Handle(
        CreatePersonCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = await HospitalStaffScope.GetRequiredProviderIdAsync(
            _context,
            request.RequestedByEmail,
            cancellationToken);

        var email = request.Email.Trim();

        var emailTaken = await _context.People
            .AnyAsync(person => person.Email == email, cancellationToken);
        if (emailTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(CreatePersonCommand.Email), "Email is already in use.")
            ]);
        }

        var staffEmailTaken = await _context.HealthcareStaff
            .AnyAsync(staff => staff.Person.Email == email, cancellationToken);
        if (staffEmailTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(CreatePersonCommand.Email), "Email is already in use by hospital staff.")
            ]);
        }

        var roles = PeopleRoles.NormalizeHospitalAssignable(request.Roles);
        var staffRole = ResolveStaffRole(roles);
        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Password = PeopleRoles.DefaultPassword,
            MustChangePassword = true,
            Gender = "Man",
            CreatedAt = DateTime.UtcNow,
            Claims = roles
                .Select(role => new UserClaim { Role = role })
                .ToList()
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync(cancellationToken);

        var staff = new HealthcareStaff
        {
            StaffCode = await CreateUniqueStaffCodeAsync(staffRole, cancellationToken),
            HealthcareProviderId = providerId,
            PersonId = person.Id,
            Specialty = staffRole == "Staff" ? "General" : null,
            Phone = "+1-555-0000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.HealthcareStaff.Add(staff);
        await _context.SaveChangesAsync(cancellationToken);

        return new PersonModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            staff.IsActive,
            roles);
    }

    private static string ResolveStaffRole(IReadOnlyList<string> roles)
    {
        if (roles.Any(role => role.Equals("Doctor", StringComparison.OrdinalIgnoreCase)))
        {
            return "Doctor";
        }

        if (roles.Any(role => role.Equals("Nurse", StringComparison.OrdinalIgnoreCase)))
        {
            return "Nurse";
        }

        return "Staff";
    }

    private async Task<string> CreateUniqueStaffCodeAsync(
        string staffRole,
        CancellationToken cancellationToken)
    {
        var prefix = staffRole switch
        {
            "Doctor" => "DOC",
            "Nurse" => "NUR",
            _ => "STF"
        };

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"{prefix}-{Random.Shared.Next(100, 999)}";
            var exists = await _context.HealthcareStaff
                .AnyAsync(staff => staff.StaffCode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        return $"{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
