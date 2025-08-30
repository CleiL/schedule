using static Schedule.Application.Dtos.Appointments.AppointmentDto;

namespace Schedule.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<ScheduleSltoDto>> ProfessionalScheduleAsync(Guid id, DateTime dia, CancellationToken ct = default);
        Task<AppointmentsReponseDto> ScheduleAsync(AppointmentCreateDto dto, CancellationToken ct=default);
        Task<IEnumerable<AppointmentsReponseDto>> GetConsultationsByProfessionalAsync(Guid id, CancellationToken ct = default);
    }
}
