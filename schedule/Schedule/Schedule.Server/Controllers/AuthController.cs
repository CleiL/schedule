using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schedule.Application.Interfaces;
using static Schedule.Application.Dtos.Authenticator.AuthDto;

namespace Schedule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService auth) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] LoginDto dto,
            CancellationToken ct)
        {
            var res = await auth.AuthenticateAsync(dto, ct);
            return Ok(res);
        }

        [HttpPost("register/patient")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<ActionResult> RegisterPatient(
            [FromBody] RegisterPatientDto dto,
            CancellationToken ct)
        {
            var ok = await auth.RegisterPatientAsync(dto, ct);
            return ok ? StatusCode(StatusCodes.Status201Created) : BadRequest();
        }

        [HttpPost("register/healthcare")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<ActionResult> RegisterHealthcare(
            [FromBody] RegisterHealthcareDto dto,
            CancellationToken ct)
        {
            var ok = await auth.RegisterHealthcareAsync(dto, ct);
            return ok ? StatusCode(StatusCodes.Status201Created) : BadRequest();
        }
    }
}