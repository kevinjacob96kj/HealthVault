using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Auth;

/// <summary>
/// Loads the current signed-in person's session (roles from the database).
/// </summary>
public record GetCurrentSessionQuery(string Email) : IRequest<LoginUserModel?>;

/// <summary>
/// Handles a <see cref="GetCurrentSessionQuery"/>.
/// </summary>
public class GetCurrentSessionHandler : IRequestHandler<GetCurrentSessionQuery, LoginUserModel?>
{
    private readonly AppDbContext _context;

    public GetCurrentSessionHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoginUserModel?> Handle(
        GetCurrentSessionQuery request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.People
            .AsNoTracking()
            .Where(person => person.Email == email)
            .Select(person => new LoginUserModel(
                person.Id,
                person.FirstName,
                person.LastName,
                person.Email,
                person.MustChangePassword,
                person.Claims
                    .Select(claim => claim.Role)
                    .OrderBy(role => role)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
