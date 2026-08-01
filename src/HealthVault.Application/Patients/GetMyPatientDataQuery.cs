using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.Patients;

public record PatientObservationModel(
    int Id,
    int LoincCodeId,
    string LoincNum,
    string ShortName,
    string LongCommonName,
    string Value,
    string? Units,
    DateTime ObservedAt,
    string? Notes);

public record GetMyPatientDataQuery(string Email)
    : IRequest<IReadOnlyList<PatientObservationModel>>;

public class GetMyPatientDataHandler
    : IRequestHandler<GetMyPatientDataQuery, IReadOnlyList<PatientObservationModel>>
{
    private readonly AppDbContext _context;

    public GetMyPatientDataHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PatientObservationModel>> Handle(
        GetMyPatientDataQuery request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return [];
        }

        var patientId = await _context.Patients
            .AsNoTracking()
            .Where(patient => patient.Person.Email == email && patient.IsActive)
            .Select(patient => (int?)patient.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (patientId is null)
        {
            return [];
        }

        return await _context.PatientData
            .AsNoTracking()
            .Where(row => row.PatientId == patientId.Value)
            .OrderByDescending(row => row.ObservedAt)
            .ThenBy(row => row.LoincCode.LoincNum)
            .Select(row => new PatientObservationModel(
                row.Id,
                row.LoincCodeId,
                row.LoincCode.LoincNum,
                row.LoincCode.ShortName ?? row.LoincCode.Component,
                row.LoincCode.LongCommonName,
                row.Value,
                row.Units ?? row.LoincCode.ExampleUnits,
                row.ObservedAt,
                row.Notes))
            .ToListAsync(cancellationToken);
    }
}
