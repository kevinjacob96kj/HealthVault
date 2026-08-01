using FluentValidation;
using FluentValidation.Results;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that sets Staff.IsActive for a person at the admin's hospital.
/// </summary>
public record UpdatePersonActiveCommand : IRequest<PersonModel>
{
    public int PersonId { get; init; }
    public string RequestedByEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Validates an <see cref="UpdatePersonActiveCommand"/>.
/// </summary>
public class UpdatePersonActiveCommandValidator
    : AbstractValidator<UpdatePersonActiveCommand>
{
    public UpdatePersonActiveCommandValidator()
    {
        RuleFor(command => command.PersonId)
            .GreaterThan(0);

        RuleFor(command => command.RequestedByEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

/// <summary>
/// Handles an <see cref="UpdatePersonActiveCommand"/>.
/// </summary>
public class UpdatePersonActiveHandler
    : IRequestHandler<UpdatePersonActiveCommand, PersonModel>
{
    private readonly AppDbContext _context;

    public UpdatePersonActiveHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PersonModel> Handle(
        UpdatePersonActiveCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = await HospitalStaffScope.GetRequiredProviderIdAsync(
            _context,
            request.RequestedByEmail,
            cancellationToken);

        var staff = await _context.HealthcareStaff
            .Include(item => item.Person)
            .ThenInclude(person => person.Claims)
            .SingleOrDefaultAsync(
                item =>
                    item.PersonId == request.PersonId &&
                    item.HealthcareProviderId == providerId,
                cancellationToken);

        if (staff is null)
        {
            throw new KeyNotFoundException(
                $"Staff for person {request.PersonId} was not found at your hospital.");
        }

        if (staff.IsActive == request.IsActive)
        {
            return ToModel(staff);
        }

        if (!request.IsActive)
        {
            if (string.Equals(
                    staff.Person.Email,
                    request.RequestedByEmail.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(UpdatePersonActiveCommand.IsActive),
                        "You cannot deactivate your own account.")
                ]);
            }

            var isAdmin = staff.Person.Claims.Any(claim =>
                claim.Role.Equals(PeopleRoles.Admin, StringComparison.OrdinalIgnoreCase));

            if (isAdmin)
            {
                var otherActiveAdmins = await _context.HealthcareStaff
                    .AsNoTracking()
                    .CountAsync(
                        item =>
                            item.HealthcareProviderId == providerId &&
                            item.PersonId != staff.PersonId &&
                            item.IsActive &&
                            item.Person.Claims.Any(claim =>
                                claim.Role == PeopleRoles.Admin),
                        cancellationToken);

                if (otherActiveAdmins == 0)
                {
                    throw new ValidationException(
                    [
                        new ValidationFailure(
                            nameof(UpdatePersonActiveCommand.IsActive),
                            "Each hospital must keep at least one active Admin.")
                    ]);
                }
            }
        }

        staff.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        return ToModel(staff);
    }

    private static PersonModel ToModel(Domain.Entities.HealthcareStaff staff)
    {
        return new PersonModel(
            staff.Person.Id,
            staff.Person.FirstName,
            staff.Person.LastName,
            staff.Person.Email,
            staff.IsActive,
            staff.Person.Claims
                .Select(claim => claim.Role)
                .OrderBy(role => role)
                .ToList());
    }
}
