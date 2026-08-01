using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Patients;

/// <summary>
/// Patient demographics returned by the API.
/// </summary>
public record PatientModel(
    int Id,
    string AbhaId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Email,
    string MobileNumber,
    bool IsActive);

/// <summary>
/// Query that gets all patients for admin views.
/// </summary>
public record GetPatientsQuery : IRequest<IReadOnlyList<PatientModel>>;

/// <summary>
/// Handles a <see cref="GetPatientsQuery"/>.
/// </summary>
public class GetPatientsHandler : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientModel>>
{
    private readonly AppDbContext _context;

    public GetPatientsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PatientModel>> Handle(
        GetPatientsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Patients
            .AsNoTracking()
            .OrderBy(patient => patient.Person.LastName)
            .ThenBy(patient => patient.Person.FirstName)
            .Select(patient => new PatientModel(
                patient.Id,
                patient.AbhaId,
                patient.Person.FirstName,
                patient.Person.LastName,
                patient.DateOfBirth,
                patient.Person.Gender,
                patient.Person.Email,
                patient.MobileNumber,
                patient.IsActive))
            .ToListAsync(cancellationToken);
    }
}
