using FluentValidation;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Command that updates a person's first and last name.
/// </summary>
public record UpdatePersonProfileCommand : IRequest<PersonModel>
{
    public int PersonId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

/// <summary>
/// Validates an <see cref="UpdatePersonProfileCommand"/>.
/// </summary>
public class UpdatePersonProfileCommandValidator
    : AbstractValidator<UpdatePersonProfileCommand>
{
    public UpdatePersonProfileCommandValidator()
    {
        RuleFor(command => command.PersonId)
            .GreaterThan(0);

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

/// <summary>
/// Handles an <see cref="UpdatePersonProfileCommand"/>.
/// </summary>
public class UpdatePersonProfileHandler
    : IRequestHandler<UpdatePersonProfileCommand, PersonModel>
{
    private readonly AppDbContext _context;

    public UpdatePersonProfileHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PersonModel> Handle(
        UpdatePersonProfileCommand request,
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

        person.FirstName = request.FirstName.Trim();
        person.LastName = request.LastName.Trim();
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
            person.Claims
                .Select(claim => claim.Role)
                .OrderBy(role => role)
                .ToList());
    }
}
