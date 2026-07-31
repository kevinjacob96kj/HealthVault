using HealthVault.Application.HealthcareProviders;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class HealthcareProvidersController : BaseApiController
{
    [Route("api/healthcare-providers")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HealthcareProviderModel>>> Get(
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(new GetHealthcareProvidersQuery(email), cancellationToken));
    }

    [Route("api/healthcare-providers")]
    [HttpPost]
    public async Task<ActionResult<HealthcareProviderModel>> Create(
        [FromBody] CreateHealthcareProviderCommand command,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            command with { RequestedByEmail = email },
            cancellationToken));
    }
}
