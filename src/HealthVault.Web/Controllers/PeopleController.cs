using HealthVault.Persistence.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
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
            .OrderBy(p => p.PersonId)
            .Select(p => new PersonDto(
                p.PersonId,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Role,
                p.Status))
            .ToListAsync(cancellationToken);

        return Ok(people);
    }
}

public record PersonDto(
    int PersonId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Status);
