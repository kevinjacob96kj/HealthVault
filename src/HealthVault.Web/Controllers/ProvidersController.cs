using HealthVault.Application.PatientDoctors;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class ProvidersController : BaseApiController
{
    [Route("api/providers")]
    [HttpGet]
    public async Task<IReadOnlyList<ProviderSearchModel>> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(new SearchProvidersQuery(q), cancellationToken);
    }

    [Route("api/providers/{providerId:int}/doctors")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorSearchModel>>> GetDoctors(
        int providerId,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            new GetProviderDoctorsQuery(providerId, email),
            cancellationToken));
    }
}
