using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schedule.Application.Dtos.Healthcares;
using Schedule.Application.Interfaces;
using static Schedule.Application.Dtos.Healthcares.HealthcareDto;

namespace Schedule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthcaresController(IHealthcareService svc) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<HealthcareResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<HealthcareDto.HealthcareResponseDto>>> GetAll(CancellationToken ct)
            => Ok(await svc.GetAllAsync(ct));

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthcareResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto?>> GetById(Guid id, CancellationToken ct)
            => Ok(await svc.GetByIdAsync(id, ct));

        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(HealthcareResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto>> Create(
            [FromBody] HealthcareDto.HealthcareCreateDto dto, CancellationToken ct)
        {
            var created = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto>> Update(
            Guid id, [FromBody] HealthcareDto.HealthcareUpdateDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest("IDs diferentes.");
            var updated = await svc.UpdateAsync(dto, ct);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("schedules")]
        public async Task<ActionResult<IEnumerable<HealthcareDto.HealthcareSchedulesResponseDto>>> GetSchedules(CancellationToken ct)
        {
            var result = await svc.GetAppointmentAsync(ct);
            return Ok(result);
        }
    }
}
