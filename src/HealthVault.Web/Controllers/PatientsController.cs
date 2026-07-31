using HealthVault.Application.PatientDoctors;
using HealthVault.Application.Patients;
using Microsoft.AspNetCore.Mvc;

namespace HealthVault.Web.Controllers;

public class PatientsController : BaseApiController
{
    [Route("api/patients")]
    [HttpGet]
    public async Task<IReadOnlyList<PatientModel>> Get(
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(new GetPatientsQuery(), cancellationToken);
    }

    [Route("api/patients/me")]
    [HttpGet]
    public async Task<ActionResult<PatientModel>> GetMine(
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var patient = await Mediator.Send(new GetMyPatientQuery(email), cancellationToken);
        if (patient is null)
        {
            return NotFound();
        }

        return patient;
    }

    [Route("api/patients/me/doctors")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssignedDoctorModel>>> GetMyDoctors(
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(new GetMyAssignedDoctorsQuery(email), cancellationToken));
    }

    [Route("api/patients/me/doctors")]
    [HttpPost]
    public async Task<ActionResult<AssignedDoctorModel>> AssignDoctor(
        [FromBody] AssignDoctorCommand command,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(await Mediator.Send(
            command with { PatientEmail = email },
            cancellationToken));
    }

    [Route("api/patients/me/doctors/{healthcareStaffId:int}")]
    [HttpDelete]
    public async Task<ActionResult> UnassignDoctor(
        int healthcareStaffId,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        await Mediator.Send(
            new UnassignDoctorCommand
            {
                PatientEmail = email,
                HealthcareStaffId = healthcareStaffId
            },
            cancellationToken);

        return NoContent();
    }
}
