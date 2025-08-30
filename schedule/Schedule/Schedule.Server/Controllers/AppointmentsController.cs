using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schedule.Application.Interfaces;
using static Schedule.Application.Dtos.Appointments.AppointmentDto;

namespace Schedule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController(IAppointmentService svc) : ControllerBase
    {
        // GET api/appointments/professional/{id}/consultations
        [HttpGet("professional/{id:guid}/consultations")]
        public async Task<ActionResult<IEnumerable<AppointmentsReponseDto>>> GetByProfessional(
            Guid id, CancellationToken ct)
            => Ok(await svc.GetConsultationsByProfessionalAsync(id, ct));

        // GET api/appointments/professional/{id}/schedule?day=2025-09-01
        [HttpGet("professional/{id:guid}/schedule")]
        public async Task<ActionResult<IEnumerable<ScheduleSltoDto>>> GetSchedule(
            Guid id,
            [FromQuery] DateOnly day,               // aceite YYYY-MM-DD
            CancellationToken ct)
        {
            // o serviço recebe DateTime; use meio-dia local só pra “ancorar” a data
            var date = day.ToDateTime(TimeOnly.MinValue);
            var slots = await svc.ProfessionalScheduleAsync(id, date, ct);
            return Ok(slots);
        }

        // POST api/appointments
        [HttpPost]
        public async Task<ActionResult<AppointmentsReponseDto>> Create(
            [FromBody] AppointmentCreateDto dto,
            CancellationToken ct)
        {
            var created = await svc.ScheduleAsync(dto, ct);
            return CreatedAtAction(nameof(GetByProfessional),
                new { id = created.HealthcareId }, created);
        }
    }
}