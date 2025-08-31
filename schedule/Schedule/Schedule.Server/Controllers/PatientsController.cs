using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schedule.Application.Interfaces;
using static Schedule.Application.Dtos.Patients.PatientDot;

namespace Schedule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController(IPatientService svc) : ControllerBase
    {
        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PatientResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetAll(CancellationToken ct)
            => Ok(await svc.GetAllAsync(ct));

        [Authorize]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PatientResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<PatientResponseDto?>> GetById(Guid id, CancellationToken ct)
            => Ok(await svc.GetByIdAsync(id, ct));

        [Authorize]
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PatientResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<PatientResponseDto>> Create(
            [FromBody] PatientCreateDto dto, CancellationToken ct)
        {
            var created = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<PatientResponseDto>> Update(
            Guid id, [FromBody] PatientUpdateDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest("IDs diferentes.");
            var updated = await svc.UpdateAsync(dto, ct);
            return Ok(updated);
        }

        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [Authorize]
        [HttpGet("schedules")]
        public async Task<ActionResult> GetSchedules(CancellationToken ct)
            => Ok(await svc.GetAppointmentAsync(ct));

        [Authorize]
        [HttpGet("patients/{id:guid}/appointments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PatientSchedulesResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientSchedulesResponseDto>> GetByPatient(Guid id, CancellationToken ct)
        {
            var dto = await svc.GetAppointmentByIdAsync(id, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

    }
}
