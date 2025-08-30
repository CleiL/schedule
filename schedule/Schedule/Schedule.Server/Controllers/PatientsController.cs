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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetAll(CancellationToken ct)
            => Ok(await svc.GetAllAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PatientResponseDto?>> GetById(Guid id, CancellationToken ct)
            => Ok(await svc.GetByIdAsync(id, ct));

        [HttpPost]
        public async Task<ActionResult<PatientResponseDto>> Create(
            [FromBody] PatientCreateDto dto, CancellationToken ct)
        {
            var created = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PatientResponseDto>> Update(
            Guid id, [FromBody] PatientUpdateDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest("IDs diferentes.");
            var updated = await svc.UpdateAsync(dto, ct);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        // GET api/patients/schedules
        // (seu PatientService monta lista de pacientes com suas consultas)
        [HttpGet("schedules")]
        public async Task<ActionResult> GetSchedules(CancellationToken ct)
            => Ok(await svc.GetAppointmentAsync(ct));
    }
}
