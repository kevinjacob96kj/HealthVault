using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

/// <summary>
/// Base controller that exposes MediatR to API controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
}
