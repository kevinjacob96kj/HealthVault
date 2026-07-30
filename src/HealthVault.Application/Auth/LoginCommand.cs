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
    }
}

/// <summary>
/// Handles a <see cref="LoginCommand"/>.
/// </summary>
public class LoginHandler : IRequestHandler<LoginCommand, LoginUserModel>
{
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

        var person = await _context.People
            .AsNoTracking()
            .Include(item => item.Claims)
            .SingleOrDefaultAsync(
                item => item.Email == email,
                cancellationToken);

        if (person is null ||
            !string.Equals(person.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(person.Password, password, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return new LoginUserModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            person.MustChangePassword,
            person.Claims
                .Select(claim => claim.Role)
                .OrderBy(role => role)
                .ToList());
    }
}
