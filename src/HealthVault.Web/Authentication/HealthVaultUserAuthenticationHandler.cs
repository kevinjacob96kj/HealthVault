using System.Security.Claims;
using System.Text.Encodings.Web;
using HealthVault.Persistence.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthVault.Web.Authentication;

public static class HealthVaultAuthDefaults
{
    public const string Scheme = "HealthVaultUser";
    public const string UserEmailHeader = "X-User-Email";
}

public class HealthVaultUserAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppDbContext _db;

    public HealthVaultUserAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HealthVaultAuthDefaults.UserEmailHeader, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var email = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthenticateResult.Fail("User email header was empty.");
        }

        var person = await _db.People
            .AsNoTracking()
            .Where(item => item.Email == email)
            .Select(item => new
            {
                item.Id,
                Roles = item.Claims.Select(claim => claim.Role).ToList()
            })
            .SingleOrDefaultAsync();

        if (person is null)
        {
            return AuthenticateResult.Fail("User was not found.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, person.Id.ToString())
        };

        foreach (var role in person.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
