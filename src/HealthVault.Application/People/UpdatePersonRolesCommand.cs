using FluentValidation;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that replaces the roles assigned to a person at the admin's hospital.
/// </summary>
public record UpdatePersonRolesCommand : IRequest<PersonModel>
{
    public int PersonId { get; init; }
    public string RequestedByEmail { get; init; } = string.Empty;
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

        RuleFor(command => command.RequestedByEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Roles)
            .Must(PeopleRoles.AreHospitalAssignable)
            .WithMessage($"Roles must be one of: {string.Join(", ", PeopleRoles.HospitalAssignable)}.");
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
        var providerId = await HospitalStaffScope.GetRequiredProviderIdAsync(
            _context,
            request.RequestedByEmail,
            cancellationToken);

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

        await HospitalStaffScope.EnsurePersonIsAtProviderAsync(
            _context,
            providerId,
            person.Id,
            cancellationToken);

        var roles = PeopleRoles.NormalizeHospitalAssignable(request.Roles);
        _context.UserClaims.RemoveRange(person.Claims);
        person.Claims = roles
            .Select(role => new UserClaim
            {
                PersonId = person.Id,
                Role = role
            })
            .ToList();

        await _context.SaveChangesAsync(cancellationToken);

        var isActive = await HospitalStaffScope.GetIsActiveAsync(
            _context,
            person.Id,
            cancellationToken);

        return new PersonModel(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            isActive,
            roles);
    }
}
