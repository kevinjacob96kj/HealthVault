using HealthVault.Application.People;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.HealthcareProviders;

public record GetHealthcareProvidersQuery(string RequestedByEmail)
    : IRequest<IReadOnlyList<HealthcareProviderModel>>;

public class GetHealthcareProvidersHandler
    : IRequestHandler<GetHealthcareProvidersQuery, IReadOnlyList<HealthcareProviderModel>>
{
    private readonly AppDbContext _context;

    public GetHealthcareProvidersHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HealthcareProviderModel>> Handle(
        GetHealthcareProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var email = request.RequestedByEmail.Trim();
        var isCentralAdmin = await _context.People
            .AsNoTracking()
            .Where(person => person.Email == email)
            .SelectMany(person => person.Claims)
            .AnyAsync(claim => claim.Role == PeopleRoles.CentralAdmin, cancellationToken);

        if (!isCentralAdmin)
        {
            throw new UnauthorizedAccessException(
                "Only a Central Admin can view all healthcare providers.");
        }

        return await _context.HealthcareProviders
            .AsNoTracking()
            .OrderBy(provider => provider.Name)
            .Select(provider => new HealthcareProviderModel(
                provider.Id,
                provider.ProviderCode,
                provider.Name,
                provider.ProviderType,
                provider.Address,
                provider.City,
                provider.State,
                provider.PostalCode,
                provider.Phone,
                provider.Email,
                provider.IsActive,
                provider.StaffMembers.Count(staff =>
                    staff.IsActive &&
                    staff.Person.Claims.Any(claim => claim.Role == PeopleRoles.Admin))))
            .ToListAsync(cancellationToken);
    }
}
