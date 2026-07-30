using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Query that gets all people and their roles.
/// </summary>
public record GetPeopleQuery : IRequest<IReadOnlyList<PersonModel>>;

/// <summary>
/// Handles a <see cref="GetPeopleQuery"/>.
/// </summary>
public class GetPeopleHandler : IRequestHandler<GetPeopleQuery, IReadOnlyList<PersonModel>>
{
    private readonly AppDbContext _context;

    public GetPeopleHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PersonModel>> Handle(
        GetPeopleQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.People
            .AsNoTracking()
            .OrderBy(person => person.Id)
            .Select(person => new PersonModel(
                person.Id,
                person.FirstName,
                person.LastName,
                person.Email,
                person.Status,
                person.Claims
                    .Select(claim => claim.Role)
                    .OrderBy(role => role)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
