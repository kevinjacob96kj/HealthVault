using FluentValidation;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that changes a person's password.
/// </summary>
public record ChangePersonPasswordCommand : IRequest
{
    public int PersonId { get; init; }
    public string? CurrentPassword { get; init; }
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Validates a <see cref="ChangePersonPasswordCommand"/>.
/// </summary>
public class ChangePersonPasswordCommandValidator
    : AbstractValidator<ChangePersonPasswordCommand>
{
    public ChangePersonPasswordCommandValidator()
    {
        RuleFor(command => command.PersonId)
            .GreaterThan(0);

        RuleFor(command => command.NewPassword)
            .StrongPassword();
    }
}

/// <summary>
/// Handles a <see cref="ChangePersonPasswordCommand"/>.
/// </summary>
public class ChangePersonPasswordHandler : IRequestHandler<ChangePersonPasswordCommand>
{
    private readonly AppDbContext _context;

    public ChangePersonPasswordHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        ChangePersonPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var person = await _context.People
            .SingleOrDefaultAsync(
                item => item.Id == request.PersonId,
                cancellationToken);

        if (person is null)
        {
            throw new KeyNotFoundException(
                $"Person {request.PersonId} was not found.");
        }

        var hasCurrentPassword = !string.IsNullOrWhiteSpace(request.CurrentPassword);

        if (hasCurrentPassword)
        {
            if (!string.Equals(
                    person.Password,
                    request.CurrentPassword!.Trim(),
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Current password is incorrect.");
            }

            person.Password = request.NewPassword.Trim();
            person.MustChangePassword = false;
        }
        else if (person.MustChangePassword)
        {
            // First sign-in: current password is not required.
            person.Password = request.NewPassword.Trim();
            person.MustChangePassword = false;
        }
        else
        {
            // Admin reset: user must change password on next sign-in.
            person.Password = request.NewPassword.Trim();
            person.MustChangePassword = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
