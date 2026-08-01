using HealthVault.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HealthVault.Web.Controllers;

public class AuthController : BaseApiController
{
    private readonly IGoogleIdentityService _googleIdentity;
    private readonly GoogleAuthOptions _googleOptions;

    public AuthController(
        IGoogleIdentityService googleIdentity,
        IOptions<GoogleAuthOptions> googleOptions)
    {
        _googleIdentity = googleIdentity;
        _googleOptions = googleOptions.Value;
    }

    [Route("api/auth/login")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<LoginUserModel> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }

    [Route("api/auth/me")]
    [HttpGet]
    public async Task<ActionResult<LoginUserModel>> Me(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var session = await Mediator.Send(new GetCurrentSessionQuery(email), cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        return session;
    }

    [Route("api/auth/login/patient/otp")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<PatientOtpSentModel> SendPatientOtp(
        [FromBody] SendPatientOtpCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }

    [Route("api/auth/login/patient")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<LoginUserModel> LoginPatient(
        [FromBody] PatientLoginCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }

    [Route("api/auth/google-config")]
    [HttpGet]
    [AllowAnonymous]
    public GoogleAuthConfigModel GetGoogleConfig()
    {
        var clientId = string.IsNullOrWhiteSpace(_googleOptions.ClientId)
            ? null
            : _googleOptions.ClientId.Trim();

        return new GoogleAuthConfigModel(clientId, _googleOptions.AllowDemoSignIn);
    }

    [Route("api/auth/signup/patient/verify")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<PatientSignupVerifiedModel> VerifyPatientSignup(
        [FromBody] VerifyPatientSignupCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(command, cancellationToken);
    }

    [Route("api/auth/signup/patient")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<LoginUserModel> CompletePatientSignup(
        [FromBody] CompletePatientSignupCommand command,
        CancellationToken cancellationToken)
    {
        return await Mediator.Send(
            command with { UsedGoogle = false },
            cancellationToken);
    }

    [Route("api/auth/signup/patient/google")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<LoginUserModel>> CompletePatientSignupWithGoogle(
        [FromBody] PatientGoogleSignupRequest request,
        CancellationToken cancellationToken)
    {
        string email;
        string firstName;
        string lastName;

        if (!string.IsNullOrWhiteSpace(request.IdToken))
        {
            var identity = await _googleIdentity.ValidateIdTokenAsync(
                request.IdToken,
                cancellationToken);
            email = identity.Email;
            firstName = identity.FirstName;
            lastName = identity.LastName;
        }
        else if (_googleOptions.AllowDemoSignIn &&
                 string.IsNullOrWhiteSpace(_googleOptions.ClientId))
        {
            if (string.IsNullOrWhiteSpace(request.DemoEmail) ||
                string.IsNullOrWhiteSpace(request.DemoFirstName) ||
                string.IsNullOrWhiteSpace(request.DemoLastName))
            {
                return BadRequest(new { title = "Enter your Google account details to continue." });
            }

            email = request.DemoEmail.Trim();
            firstName = request.DemoFirstName.Trim();
            lastName = request.DemoLastName.Trim();
        }
        else
        {
            return BadRequest(new { title = "Google sign-in is not available." });
        }

        var user = await Mediator.Send(
            new CompletePatientSignupCommand
            {
                SignupToken = request.SignupToken,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Man" : request.Gender,
                MobileNumber = request.MobileNumber ?? string.Empty,
                UsedGoogle = true
            },
            cancellationToken);

        return Ok(user);
    }
}

public record PatientGoogleSignupRequest(
    string SignupToken,
    string? IdToken,
    string? DemoEmail,
    string? DemoFirstName,
    string? DemoLastName,
    string? Gender,
    string? MobileNumber);
