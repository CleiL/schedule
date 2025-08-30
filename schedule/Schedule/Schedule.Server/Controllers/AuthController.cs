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
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] LoginDto dto,
            CancellationToken ct)
        {
            var res = await auth.AuthenticateAsync(dto, ct);
            return Ok(res);
        }

        [HttpPost("register/patient")]
        public async Task<ActionResult> RegisterPatient(
            [FromBody] RegisterPatientDto dto,
            CancellationToken ct)
        {
            var ok = await auth.RegisterPatientAsync(dto, ct);
            return ok ? StatusCode(StatusCodes.Status201Created) : BadRequest();
        }

        [HttpPost("register/healthcare")]
        public async Task<ActionResult> RegisterHealthcare(
            [FromBody] RegisterHealthcareDto dto,
            CancellationToken ct)
        {
            var ok = await auth.RegisterHealthcareAsync(dto, ct);
            return ok ? StatusCode(StatusCodes.Status201Created) : BadRequest();
        }

        // opcional: confirmação “no-op” que você já tem no serviço
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(
            [FromBody] RegisterResponseDto dto,
            CancellationToken ct)
        {
            await auth.ConfirmRegisterAsync(dto, ct);
            return Ok();
        }
    }
}