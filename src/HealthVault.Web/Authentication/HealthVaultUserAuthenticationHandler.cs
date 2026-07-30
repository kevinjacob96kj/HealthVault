using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
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
    public HealthVaultUserAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HealthVaultAuthDefaults.UserEmailHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(AuthenticateResult.Fail("User email header was empty."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email)
            ],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
