using HealthVault.Application.People;
using HealthVault.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class PeopleController : BaseApiController
{
    [Route("api/people")]
    [HttpGet]
    [Authorize(Policy = HealthVaultPolicies.MustBeAnAdmin)]
    public async Task<ActionResult<IReadOnlyList<PersonModel>>> Get(
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(new GetPeopleQuery(email), cancellationToken));
    }

    [Route("api/people/roles")]
    [HttpGet]
    [Authorize(Policy = HealthVaultPolicies.MustBeAnAdmin)]
    public async Task<IReadOnlyList<string>> GetRoles(
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableRolesQuery();
        return await Mediator.Send(query, cancellationToken);
    }

    [Route("api/people")]
    [HttpPost]
    [Authorize(Policy = HealthVaultPolicies.MustBeAnAdmin)]
    public async Task<ActionResult<PersonModel>> Create(
        [FromBody] CreatePersonCommand command,
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

    [Route("api/people/{personId:int}/roles")]
    [HttpPut]
    [Authorize(Policy = HealthVaultPolicies.MustBeAnAdmin)]
    public async Task<ActionResult<PersonModel>> UpdateRoles(
        int personId,
        [FromBody] UpdatePersonRolesCommand command,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            command with
            {
                PersonId = personId,
                RequestedByEmail = email
            },
            cancellationToken));
    }

    [Route("api/people/{personId:int}/active")]
    [HttpPut]
    [Authorize(Policy = HealthVaultPolicies.MustBeAnAdmin)]
    public async Task<ActionResult<PersonModel>> UpdateActive(
        int personId,
        [FromBody] UpdatePersonActiveCommand command,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            command with
            {
                PersonId = personId,
                RequestedByEmail = email
            },
            cancellationToken));
    }

    [Route("api/people/{personId:int}/password")]
    [HttpPut]
    public async Task ChangePassword(
        int personId,
        [FromBody] ChangePersonPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            command with { PersonId = personId },
            cancellationToken);
    }

    [Route("api/people/{personId:int}/profile")]
    [HttpPut]
    public async Task<PersonModel> UpdateProfile(
        int personId,
        [FromBody] UpdatePersonProfileCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(
            command with { PersonId = personId },
            cancellationToken);
    }
}
