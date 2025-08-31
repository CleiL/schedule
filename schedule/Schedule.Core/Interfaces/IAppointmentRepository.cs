using Schedule.Core.Entities;

namespace Schedule.Core.Interfaces
{
    public interface IAppointmentRepository
        : IBaseRepository<Appointment>
    {
        /// <summary>
        /// Existe um agendamento para o profissional de saúde em um horário específico.
        /// </summary>
        /// <param name="healthcareId"></param>
        /// <param name="hour"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> ExistsHealthcareHour(Guid healthcareId, DateTime hour, CancellationToken ct = default);
        /// <summary>
        /// Existe um agendamento para o paciente em um dia específico.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="day"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> ExistsPatientAppointment(Guid patientId, DateOnly day, CancellationToken ct = default);
        /// <summary>
        /// Consulta todos os agendamentos de um profissional de saúde.
        /// </summary>
        /// <param name="healthcareId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Appointment>> GetByHealthcareAsync(Guid healthcareId, CancellationToken ct = default);
        /// <summary>
        /// Obtém todos os horários ocupados de um profissional de saúde.
        /// </summary>
        /// <param name="healthcareId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<DateTime>> GetProfessionalBusySchedules(Guid healthcareId, DateOnly day, CancellationToken ct = default);

        Task<bool> ExistsPatientAppointmentWithHealthcare( Guid patientId, Guid healthcareId, DateOnly day, CancellationToken ct = default);
        Task<bool> ExistsPatientAnyAsync(Guid patientId, CancellationToken ct = default);

        /// <summary>
        /// Consulta todos os agendamentos de um profissional de saúde.
        /// </summary>
        /// <param name="healthcareId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken ct = default);
    }
}
