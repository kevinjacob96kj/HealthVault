using FluentValidation;
using FluentValidation.Results;
using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.PatientDoctors;

public record ProviderSearchModel(
    int Id,
    string ProviderCode,
    string Name,
    string ProviderType,
    string City,
    string State);

public record DoctorSearchModel(
    int Id,
    string StaffCode,
    string FirstName,
    string LastName,
    string Email,
    string? Specialty,
    bool IsAssigned);

public record AssignedDoctorModel(
    int AssignmentId,
    int HealthcareStaffId,
    string FirstName,
    string LastName,
    string Email,
    string? Specialty,
    string ProviderName,
    bool IsActive,
    DateTime AssignedAt,
    DateTime? UnassignedAt,
    string? Notes);

public record SearchProvidersQuery(string? Query) : IRequest<IReadOnlyList<ProviderSearchModel>>;

public class SearchProvidersHandler
    : IRequestHandler<SearchProvidersQuery, IReadOnlyList<ProviderSearchModel>>
{
    private readonly AppDbContext _context;

    public SearchProvidersHandler(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProviderSearchModel>> Handle(
        SearchProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var term = request.Query?.Trim() ?? string.Empty;
        var query = _context.HealthcareProviders.AsNoTracking().Where(provider => provider.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(provider =>
                provider.Name.Contains(term) ||
                provider.ProviderCode.Contains(term) ||
                provider.City.Contains(term));
        }

        return await query
            .OrderBy(provider => provider.Name)
            .Take(50)
            .Select(provider => new ProviderSearchModel(
                provider.Id,
                provider.ProviderCode,
                provider.Name,
                provider.ProviderType,
                provider.City,
                provider.State))
            .ToListAsync(cancellationToken);
    }
}

public record GetProviderDoctorsQuery(int ProviderId, string PatientEmail)
    : IRequest<IReadOnlyList<DoctorSearchModel>>;

public class GetProviderDoctorsHandler
    : IRequestHandler<GetProviderDoctorsQuery, IReadOnlyList<DoctorSearchModel>>
{
    private readonly AppDbContext _context;

    public GetProviderDoctorsHandler(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<DoctorSearchModel>> Handle(
        GetProviderDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = await ResolvePatientIdAsync(request.PatientEmail, cancellationToken);

        return await _context.HealthcareStaff
            .AsNoTracking()
            .Where(staff =>
                staff.HealthcareProviderId == request.ProviderId &&
                staff.IsActive &&
                staff.Person.Claims.Any(claim => claim.Role == "Doctor"))
            .OrderBy(staff => staff.Person.LastName)
            .ThenBy(staff => staff.Person.FirstName)
            .Select(staff => new DoctorSearchModel(
                staff.Id,
                staff.StaffCode,
                staff.Person.FirstName,
                staff.Person.LastName,
                staff.Person.Email,
                staff.Specialty,
                staff.PatientAssignments.Any(assignment =>
                    assignment.PatientId == patientId && assignment.IsActive)))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ResolvePatientIdAsync(string email, CancellationToken cancellationToken)
    {
        var patientId = await _context.Patients
            .AsNoTracking()
            .Where(patient => patient.Person.Email == email.Trim())
            .Select(patient => (int?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (patientId is null)
        {
            throw new UnauthorizedAccessException("No patient record is linked to your account.");
        }

        return patientId.Value;
    }
}

public record GetMyAssignedDoctorsQuery(string PatientEmail)
    : IRequest<IReadOnlyList<AssignedDoctorModel>>;

public class GetMyAssignedDoctorsHandler
    : IRequestHandler<GetMyAssignedDoctorsQuery, IReadOnlyList<AssignedDoctorModel>>
{
    private readonly AppDbContext _context;

    public GetMyAssignedDoctorsHandler(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<AssignedDoctorModel>> Handle(
        GetMyAssignedDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = await _context.Patients
            .AsNoTracking()
            .Where(patient => patient.Person.Email == request.PatientEmail.Trim())
            .Select(patient => (int?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (patientId is null)
        {
            throw new UnauthorizedAccessException("No patient record is linked to your account.");
        }

        return await _context.PatientDoctorAssignments
            .AsNoTracking()
            .Where(assignment => assignment.PatientId == patientId.Value)
            .OrderByDescending(assignment => assignment.IsActive)
            .ThenByDescending(assignment => assignment.AssignedAt)
            .Select(assignment => new AssignedDoctorModel(
                assignment.Id,
                assignment.HealthcareStaffId,
                assignment.Doctor.Person.FirstName,
                assignment.Doctor.Person.LastName,
                assignment.Doctor.Person.Email,
                assignment.Doctor.Specialty,
                assignment.Doctor.HealthcareProvider.Name,
                assignment.IsActive,
                assignment.AssignedAt,
                assignment.UnassignedAt,
                assignment.Notes))
            .ToListAsync(cancellationToken);
    }
}

public record AssignDoctorCommand : IRequest<AssignedDoctorModel>
{
    public string PatientEmail { get; init; } = string.Empty;
    public int HealthcareStaffId { get; init; }
    public bool ConsentAccepted { get; init; }
    public string? Notes { get; init; }
}

public class AssignDoctorCommandValidator : AbstractValidator<AssignDoctorCommand>
{
    public AssignDoctorCommandValidator()
    {
        RuleFor(command => command.PatientEmail).NotEmpty().EmailAddress();
        RuleFor(command => command.HealthcareStaffId).GreaterThan(0);
        RuleFor(command => command.ConsentAccepted)
            .Equal(true)
            .WithMessage("You must agree to share your data with this doctor.");
        RuleFor(command => command.Notes).MaximumLength(500);
    }
}

public class AssignDoctorHandler : IRequestHandler<AssignDoctorCommand, AssignedDoctorModel>
{
    private readonly AppDbContext _context;

    public AssignDoctorHandler(AppDbContext context) => _context = context;

    public async Task<AssignedDoctorModel> Handle(
        AssignDoctorCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.ConsentAccepted)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(AssignDoctorCommand.ConsentAccepted),
                    "You must agree to share your data with this doctor.")
            ]);
        }

        var email = request.PatientEmail.Trim();

        var patient = await _context.Patients
            .SingleOrDefaultAsync(item => item.Person.Email == email, cancellationToken)
            ?? throw new UnauthorizedAccessException("No patient record is linked to your account.");

        var doctor = await _context.HealthcareStaff
            .Include(staff => staff.HealthcareProvider)
            .Include(staff => staff.Person)
                .ThenInclude(person => person.Claims)
            .SingleOrDefaultAsync(staff => staff.Id == request.HealthcareStaffId, cancellationToken);

        if (doctor is null ||
            !doctor.IsActive ||
            !doctor.Person.Claims.Any(claim =>
                claim.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(AssignDoctorCommand.HealthcareStaffId),
                    "Choose an active doctor.")
            ]);
        }

        var existingActive = await _context.PatientDoctorAssignments
            .AnyAsync(
                assignment =>
                    assignment.PatientId == patient.Id &&
                    assignment.HealthcareStaffId == doctor.Id &&
                    assignment.IsActive,
                cancellationToken);

        if (existingActive)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(AssignDoctorCommand.HealthcareStaffId),
                    "This doctor is already assigned to you.")
            ]);
        }

        var assignment = new PatientDoctorAssignment
        {
            PatientId = patient.Id,
            HealthcareStaffId = doctor.Id,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        _context.PatientDoctorAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return new AssignedDoctorModel(
            assignment.Id,
            doctor.Id,
            doctor.Person.FirstName,
            doctor.Person.LastName,
            doctor.Person.Email,
            doctor.Specialty,
            doctor.HealthcareProvider.Name,
            true,
            assignment.AssignedAt,
            null,
            assignment.Notes);
    }
}

public record UnassignDoctorCommand : IRequest
{
    public string PatientEmail { get; init; } = string.Empty;
    public int HealthcareStaffId { get; init; }
}

public class UnassignDoctorCommandValidator : AbstractValidator<UnassignDoctorCommand>
{
    public UnassignDoctorCommandValidator()
    {
        RuleFor(command => command.PatientEmail).NotEmpty().EmailAddress();
        RuleFor(command => command.HealthcareStaffId).GreaterThan(0);
    }
}

public class UnassignDoctorHandler : IRequestHandler<UnassignDoctorCommand>
{
    private readonly AppDbContext _context;

    public UnassignDoctorHandler(AppDbContext context) => _context = context;

    public async Task Handle(UnassignDoctorCommand request, CancellationToken cancellationToken)
    {
        var email = request.PatientEmail.Trim();

        var patientId = await _context.Patients
            .Where(patient => patient.Person.Email == email)
            .Select(patient => (int?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("No patient record is linked to your account.");

        var assignment = await _context.PatientDoctorAssignments
            .SingleOrDefaultAsync(
                item =>
                    item.PatientId == patientId &&
                    item.HealthcareStaffId == request.HealthcareStaffId &&
                    item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Active assignment was not found.");

        assignment.IsActive = false;
        assignment.UnassignedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DoctorPatientModel(
    int Id,
    string AbhaId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Email,
    string MobileNumber,
    DateTime AssignedAt,
    string? Notes);

public record GetMyPatientsQuery(string DoctorEmail) : IRequest<IReadOnlyList<DoctorPatientModel>>;

public class GetMyPatientsHandler
    : IRequestHandler<GetMyPatientsQuery, IReadOnlyList<DoctorPatientModel>>
{
    private readonly AppDbContext _context;

    public GetMyPatientsHandler(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<DoctorPatientModel>> Handle(
        GetMyPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var staffId = await _context.HealthcareStaff
            .AsNoTracking()
            .Where(staff =>
                staff.Person.Email == request.DoctorEmail.Trim() &&
                staff.IsActive &&
                staff.Person.Claims.Any(claim => claim.Role == "Doctor"))
            .Select(staff => (int?)staff.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (staffId is null)
        {
            throw new UnauthorizedAccessException("Your account is not linked to an active doctor profile.");
        }

        return await _context.PatientDoctorAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.HealthcareStaffId == staffId.Value &&
                assignment.IsActive)
            .OrderBy(assignment => assignment.Patient.Person.LastName)
            .ThenBy(assignment => assignment.Patient.Person.FirstName)
            .Select(assignment => new DoctorPatientModel(
                assignment.Patient.Id,
                assignment.Patient.AbhaId,
                assignment.Patient.Person.FirstName,
                assignment.Patient.Person.LastName,
                assignment.Patient.DateOfBirth,
                assignment.Patient.Person.Gender,
                assignment.Patient.Person.Email,
                assignment.Patient.MobileNumber,
                assignment.AssignedAt,
                assignment.Notes))
            .ToListAsync(cancellationToken);
    }
}
