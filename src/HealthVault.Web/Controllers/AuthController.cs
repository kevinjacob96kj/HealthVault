using HealthVault.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class AuthController : BaseApiController
{
    [Route("api/auth/login")]
    [HttpPost]
    public async Task<LoginUserModel> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }
}
