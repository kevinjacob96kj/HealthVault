using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Query that gets people for the signed-in admin's hospital only.
/// </summary>
public record GetPeopleQuery(string RequestedByEmail) : IRequest<IReadOnlyList<PersonModel>>;

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
        var providerId = await HospitalStaffScope.GetRequiredProviderIdAsync(
            _context,
            request.RequestedByEmail,
            cancellationToken);

        return await _context.HealthcareStaff
            .AsNoTracking()
            .Where(staff => staff.HealthcareProviderId == providerId)
            .OrderBy(staff => staff.PersonId)
            .Select(staff => new PersonModel(
                staff.Person.Id,
                staff.Person.FirstName,
                staff.Person.LastName,
                staff.Person.Email,
                staff.IsActive,
                staff.Person.Claims
                    .Select(claim => claim.Role)
                    .OrderBy(role => role)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
