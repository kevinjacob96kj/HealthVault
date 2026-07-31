using HealthVault.Application.PatientDoctors;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class DoctorsController : BaseApiController
{
    [Route("api/doctors/me/patients")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorPatientModel>>> GetMyPatients(
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(new GetMyPatientsQuery(email), cancellationToken));
    }
}
