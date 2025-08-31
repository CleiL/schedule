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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AppointmentsResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<AppointmentsResponseDto>>> GetByProfessional( Guid id, CancellationToken ct)
            => Ok(await svc.GetConsultationsByProfessionalAsync(id, ct));

        // GET api/appointments/professional/{id}/schedule?day=2025-09-01
        [HttpGet("professional/{id:guid}/schedule")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ScheduleSltoDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<ScheduleSltoDto>>> GetSchedule( Guid id, [FromQuery] DateOnly day, CancellationToken ct)
        {
            // o serviço recebe DateTime; use meio-dia local só pra “ancorar” a data
            var date = day.ToDateTime(TimeOnly.MinValue);
            var slots = await svc.ProfessionalScheduleAsync(id, date, ct);
            return Ok(slots);
        }

        // POST api/appointments
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AppointmentsResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<AppointmentsResponseDto>> Create( [FromBody] AppointmentCreateDto dto, CancellationToken ct)
        {
            var created = await svc.ScheduleAsync(dto, ct);
            return Ok(created);
        }
    }
}