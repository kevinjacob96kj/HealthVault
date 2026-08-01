using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Patients;

/// <summary>
/// Query that gets the patient record for the signed-in user's email.
/// </summary>
public record GetMyPatientQuery(string Email) : IRequest<PatientModel?>;

/// <summary>
/// Handles a <see cref="GetMyPatientQuery"/>.
/// </summary>
public class GetMyPatientHandler : IRequestHandler<GetMyPatientQuery, PatientModel?>
{
    private readonly AppDbContext _context;

    public GetMyPatientHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PatientModel?> Handle(
        GetMyPatientQuery request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Patients
            .AsNoTracking()
            .Where(patient => patient.Person.Email == email)
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
            .SingleOrDefaultAsync(cancellationToken);
    }
}
