using FluentValidation;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that replaces the roles assigned to a person.
/// </summary>
public record UpdatePersonRolesCommand : IRequest<PersonModel>
{
    public int PersonId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>
/// Validates an <see cref="UpdatePersonRolesCommand"/>.
/// </summary>
public class UpdatePersonRolesCommandValidator
    : AbstractValidator<UpdatePersonRolesCommand>
{
    public UpdatePersonRolesCommandValidator()
    {
        RuleFor(command => command.PersonId)
            .GreaterThan(0);

        RuleFor(command => command.Roles)
            .Must(PeopleRoles.AreValid)
            .WithMessage($"Roles must be one of: {string.Join(", ", PeopleRoles.Allowed)}.");
    }
}

/// <summary>
/// Handles an <see cref="UpdatePersonRolesCommand"/>.
/// </summary>
public class UpdatePersonRolesHandler
    : IRequestHandler<UpdatePersonRolesCommand, PersonModel>
{
    private readonly AppDbContext _context;

    public UpdatePersonRolesHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PersonModel> Handle(
        UpdatePersonRolesCommand request,
        CancellationToken cancellationToken)
    {
        var person = await _context.People
            .Include(item => item.Claims)
            .SingleOrDefaultAsync(
                item => item.Id == request.PersonId,
                cancellationToken);

        if (person is null)
        {
            throw new KeyNotFoundException(
                $"Person {request.PersonId} was not found.");
        }

        var roles = PeopleRoles.Normalize(request.Roles);
        _context.UserClaims.RemoveRange(person.Claims);
        person.Claims = roles
            .Select(role => new UserClaim
            {
                PersonId = person.Id,
                Role = role
            })
            .ToList();

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
