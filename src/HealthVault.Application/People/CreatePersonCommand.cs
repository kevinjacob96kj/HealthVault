using FluentValidation;
using FluentValidation.Results;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that creates a person.
/// </summary>
public record CreatePersonCommand : IRequest<PersonModel>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = "Active";
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

        RuleFor(command => command.Status)
            .Must(status => status is "Active" or "Inactive")
            .WithMessage("Status must be Active or Inactive.");

        RuleFor(command => command.Roles)
            .Must(PeopleRoles.AreValid)
            .WithMessage($"Roles must be one of: {string.Join(", ", PeopleRoles.Allowed)}.");
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

        var roles = PeopleRoles.Normalize(request.Roles);
        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Password = PeopleRoles.DefaultPassword,
            MustChangePassword = true,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            Claims = roles
                .Select(role => new UserClaim { Role = role })
                .ToList()
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync(cancellationToken);

        return new PersonModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            person.Status,
            roles);
    }
}
