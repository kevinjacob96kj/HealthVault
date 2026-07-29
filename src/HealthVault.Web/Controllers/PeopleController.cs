using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Doctor",
        "Nurse",
        "Patient",
        "Staff"
    };

    private readonly AppDbContext _db;

    public PeopleController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonDto>>> GetPeople(CancellationToken cancellationToken)
    {
        var people = await _db.People
            .AsNoTracking()
            .Include(p => p.Claims)
            .OrderBy(p => p.Id)
            .Select(p => new PersonDto(
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Status,
                p.Claims.Select(c => c.Role).OrderBy(r => r).ToList()))
            .ToListAsync(cancellationToken);

        return Ok(people);
    }

    [HttpGet("roles")]
    public ActionResult<IEnumerable<string>> GetAvailableRoles()
    {
        return Ok(AllowedRoles.OrderBy(r => r).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PersonDto>> CreatePerson(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("First name, last name, and email are required.");
        }

        var roles = NormalizeRoles(request.Roles);
        if (roles is null)
        {
            return BadRequest($"Roles must be one of: {string.Join(", ", AllowedRoles.OrderBy(r => r))}.");
        }

        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedAt = DateTime.UtcNow,
            Claims = roles.Select(role => new UserClaim { Role = role }).ToList()
        };

        _db.People.Add(person);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new PersonDto(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            person.Status,
            person.Claims.Select(c => c.Role).OrderBy(r => r).ToList());

        return Created($"/api/people/{person.Id}", dto);
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<PersonDto>> UpdateRoles(
        int id,
        [FromBody] UpdateRolesRequest request,
        CancellationToken cancellationToken)
    {
        var person = await _db.People
            .Include(p => p.Claims)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (person is null)
        {
            return NotFound();
        }

        var roles = NormalizeRoles(request.Roles);
        if (roles is null)
        {
            return BadRequest($"Roles must be one of: {string.Join(", ", AllowedRoles.OrderBy(r => r))}.");
        }

        _db.UserClaims.RemoveRange(person.Claims);
        person.Claims = roles.Select(role => new UserClaim { PersonId = id, Role = role }).ToList();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new PersonDto(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Email,
            person.Status,
            person.Claims.Select(c => c.Role).OrderBy(r => r).ToList()));
    }

    private static List<string>? NormalizeRoles(IEnumerable<string>? roles)
    {
        var normalized = (roles ?? Enumerable.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => AllowedRoles.FirstOrDefault(a => a.Equals(r.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(r => r is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invalid = (roles ?? Enumerable.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Any(r => !AllowedRoles.Contains(r.Trim()));

        return invalid ? null : normalized;
    }
}

public record PersonDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    IReadOnlyList<string> Roles);

public record CreatePersonRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Status,
    IReadOnlyList<string>? Roles);

public record UpdateRolesRequest(IReadOnlyList<string>? Roles);
