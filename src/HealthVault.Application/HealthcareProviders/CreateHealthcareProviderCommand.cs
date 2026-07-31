using FluentValidation;
using FluentValidation.Results;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.HealthcareProviders;

public record HealthcareProviderModel(
    int Id,
    string ProviderCode,
    string Name,
    string ProviderType,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email,
    bool IsActive,
    int AdminCount);

public record CreateHealthcareProviderCommand : IRequest<HealthcareProviderModel>
{
    public string RequestedByEmail { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ProviderType { get; init; } = "Hospital";
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }

    public string AdminFirstName { get; init; } = string.Empty;
    public string AdminLastName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
}

public class CreateHealthcareProviderCommandValidator
    : AbstractValidator<CreateHealthcareProviderCommand>
{
    private static readonly HashSet<string> ProviderTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hospital",
        "Clinic",
        "Lab",
        "Pharmacy"
    };

    public CreateHealthcareProviderCommandValidator()
    {
        RuleFor(command => command.RequestedByEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.ProviderCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.ProviderType)
            .Must(type => ProviderTypes.Contains(type))
            .WithMessage("Provider type must be Hospital, Clinic, Lab, or Pharmacy.");

        RuleFor(command => command.Address).NotEmpty().MaximumLength(256);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.State).NotEmpty().MaximumLength(50);
        RuleFor(command => command.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(30);
        RuleFor(command => command.Email).MaximumLength(256)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public class CreateHealthcareProviderHandler
    : IRequestHandler<CreateHealthcareProviderCommand, HealthcareProviderModel>
{
    private readonly AppDbContext _context;

    public CreateHealthcareProviderHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthcareProviderModel> Handle(
        CreateHealthcareProviderCommand request,
        CancellationToken cancellationToken)
    {
        await EnsureCentralAdminAsync(request.RequestedByEmail, cancellationToken);

        var providerCode = request.ProviderCode.Trim().ToUpperInvariant();
        var adminEmail = request.AdminEmail.Trim();

        if (await _context.HealthcareProviders.AnyAsync(
                provider => provider.ProviderCode == providerCode,
                cancellationToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateHealthcareProviderCommand.ProviderCode),
                    "Provider code is already in use.")
            ]);
        }

        if (await _context.People.AnyAsync(person => person.Email == adminEmail, cancellationToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateHealthcareProviderCommand.AdminEmail),
                    "Admin email is already in use.")
            ]);
        }

        var provider = new HealthcareProvider
        {
            ProviderCode = providerCode,
            Name = request.Name.Trim(),
            ProviderType = NormalizeProviderType(request.ProviderType),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var admin = new Person
        {
            FirstName = request.AdminFirstName.Trim(),
            LastName = request.AdminLastName.Trim(),
            Email = adminEmail,
            Password = People.PeopleRoles.DefaultPassword,
            MustChangePassword = true,
            Gender = "Man",
            CreatedAt = DateTime.UtcNow,
            Claims =
            [
                new UserClaim { Role = People.PeopleRoles.Admin },
                new UserClaim { Role = "Staff" }
            ]
        };

        _context.HealthcareProviders.Add(provider);
        _context.People.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        var staff = new HealthcareStaff
        {
            StaffCode = await CreateUniqueStaffCodeAsync(cancellationToken),
            HealthcareProviderId = provider.Id,
            PersonId = admin.Id,
            Specialty = "Administration",
            Phone = request.Phone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.HealthcareStaff.Add(staff);
        await _context.SaveChangesAsync(cancellationToken);

        return new HealthcareProviderModel(
            provider.Id,
            provider.ProviderCode,
            provider.Name,
            provider.ProviderType,
            provider.Address,
            provider.City,
            provider.State,
            provider.PostalCode,
            provider.Phone,
            provider.Email,
            provider.IsActive,
            AdminCount: 1);
    }

    private async Task EnsureCentralAdminAsync(
        string requestedByEmail,
        CancellationToken cancellationToken)
    {
        var email = requestedByEmail.Trim();
        var isCentralAdmin = await _context.People
            .AsNoTracking()
            .Where(person => person.Email == email)
            .SelectMany(person => person.Claims)
            .AnyAsync(claim => claim.Role == People.PeopleRoles.CentralAdmin, cancellationToken);

        if (!isCentralAdmin)
        {
            throw new UnauthorizedAccessException(
                "Only a Central Admin can create healthcare providers.");
        }
    }

    private static string NormalizeProviderType(string providerType)
    {
        return providerType.Trim() switch
        {
            var value when value.Equals("Hospital", StringComparison.OrdinalIgnoreCase) => "Hospital",
            var value when value.Equals("Clinic", StringComparison.OrdinalIgnoreCase) => "Clinic",
            var value when value.Equals("Lab", StringComparison.OrdinalIgnoreCase) => "Lab",
            _ => "Pharmacy"
        };
    }

    private async Task<string> CreateUniqueStaffCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"STF-{Random.Shared.Next(100, 999)}";
            var exists = await _context.HealthcareStaff
                .AnyAsync(staff => staff.StaffCode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        return $"STF-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
