using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schedule.Application.Dtos.Healthcares;
using Schedule.Application.Interfaces;

namespace Schedule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthcaresController(IHealthcareService svc) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HealthcareDto.HealthcareResponseDto>>> GetAll(CancellationToken ct)
            => Ok(await svc.GetAllAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto?>> GetById(Guid id, CancellationToken ct)
            => Ok(await svc.GetByIdAsync(id, ct));

        [HttpPost]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto>> Create(
            [FromBody] HealthcareDto.HealthcareCreateDto dto, CancellationToken ct)
        {
            var created = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<HealthcareDto.HealthcareResponseDto>> Update(
            Guid id, [FromBody] HealthcareDto.HealthcareUpdateDto dto, CancellationToken ct)
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

        // GET api/healthcares/schedules
        [HttpGet("schedules")]
        public async Task<ActionResult> GetSchedules(CancellationToken ct)
            => Ok(await svc.GetAppintmentAsync(ct));
    }
}
