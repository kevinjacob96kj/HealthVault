using MediatR;

namespace HealthVault.Application.People;

/// <summary>
/// Query that gets the roles available for a person.
/// </summary>
public record GetAvailableRolesQuery : IRequest<IReadOnlyList<string>>;

/// <summary>
/// Handles a <see cref="GetAvailableRolesQuery"/>.
/// </summary>
public class GetAvailableRolesHandler
    : IRequestHandler<GetAvailableRolesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(
        GetAvailableRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = PeopleRoles.Allowed
            .OrderBy(role => role)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(roles);
    }
}
