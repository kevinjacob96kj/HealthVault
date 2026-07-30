using HealthVault.Application.People;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class PeopleController : BaseApiController
{
    [Route("api/people")]
    [HttpGet]
    public async Task<IReadOnlyList<PersonModel>> Get(
        CancellationToken cancellationToken)
    {
        var query = new GetPeopleQuery();
        return await Mediator.Send(query, cancellationToken);
    }

    [Route("api/people/roles")]
    [HttpGet]
    public async Task<IReadOnlyList<string>> GetRoles(
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableRolesQuery();
        return await Mediator.Send(query, cancellationToken);
    }

    [Route("api/people")]
    [HttpPost]
    public async Task<PersonModel> Create(
        [FromBody] CreatePersonCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }

    [Route("api/people/{personId:int}/roles")]
    [HttpPut]
    public async Task<PersonModel> UpdateRoles(
        int personId,
        [FromBody] UpdatePersonRolesCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(
            command with { PersonId = personId },
            cancellationToken);
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
