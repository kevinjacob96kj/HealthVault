using HealthVault.Application.PatientDoctors;
using HealthVault.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class DoctorsController : BaseApiController
{
    [Route("api/doctors/me/patients")]
    [HttpGet]
    [Authorize(Policy = HealthVaultPolicies.MustBeADoctor)]
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

    [Route("api/doctors/me/patients/{patientId:int}")]
    [HttpGet]
    [Authorize(Policy = HealthVaultPolicies.MustBeADoctor)]
    public async Task<ActionResult<DoctorPatientModel>> GetMyPatientCase(
        int patientId,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            new GetDoctorPatientCaseQuery(email, patientId),
            cancellationToken));
    }
}
