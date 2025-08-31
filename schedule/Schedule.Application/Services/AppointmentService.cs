using Microsoft.Extensions.Logging;
using Schedule.Application.Dtos.Appointments;
using Schedule.Application.Interfaces;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Threading;
using static Schedule.Application.Dtos.Appointments.AppointmentDto;

namespace Schedule.Application.Services
{
    public class AppointmentService
        (
            IAppointmentRepository repository,
            ILogger<AppointmentService> logger,
            IUnitOfWorkFactory uowFactory
        )
        : IAppointmentService
    {
        private readonly IAppointmentRepository _repository = repository;
        private readonly ILogger<AppointmentService> _logger = logger;
        private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

        private const int SlotMinutes = 30;
        private static readonly TimeOnly Inicio = new(8, 0);
        private static readonly TimeOnly FimExclusivo = new(18, 0);


        public async Task<IEnumerable<AppointmentsResponseDto>> GetConsultationsByProfessionalAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Consulta.GetConsultasPorProfissional",
                ["MedicoId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var consultas = await _repository.GetByHealthcareAsync(id, ct);

                    await uow.CommitAsync(ct);

                    return consultas.Select(c => new AppointmentsResponseDto
                    (
                        c.HealthcareId,
                        c.PatientId,
                        DateOnly.FromDateTime(c.StartAt),
                        c.StartAt
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao listar consultas do médico {MedicoId}", id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<ScheduleSltoDto>> ProfessionalScheduleAsync(Guid id, DateTime dia, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Consulta.AgendaDoProfissionalAsync",
                ["HealthcareId"] = id,
                ["Dia"] = dia.Date
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var slots = GerarSlots(dia);

                    // ✅ agora o repo já filtra por dia
                    var ocupados = await _repository.GetProfessionalBusySchedules(id, DateOnly.FromDateTime(dia), ct);
                    var setOcupados = new HashSet<DateTime>(ocupados);

                    var agenda = slots.Select(h =>
                        new ScheduleSltoDto(h, !setOcupados.Contains(h))
                    ).ToList();

                    await uow.CommitAsync(ct);
                    return agenda;
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<AppointmentsResponseDto> ScheduleAsync(AppointmentCreateDto dto, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Consulta.AgendarAsync",
                ["HealthcareId"] = dto.HealthcareId,
                ["PatientId"] = dto.PatientId,
                ["Day"] = dto.Day,
                ["Hour"] = dto.Hour
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    // Combina Day (DateOnly) + Hour (DateTime -> TimeOnly)
                    var startAtRaw = dto.Day.ToDateTime(TimeOnly.FromDateTime(dto.Hour));
                    var startAt = ToLocalUnspecified(startAtRaw);

                    // Regras de negócio
                    ValidarDiaUtil(startAt);
                    ValidarJanela(startAt);
                    ValidarMultiploDe30(startAt);

                    // Conflitos
                    if (await _repository.ExistsHealthcareHour(dto.HealthcareId, startAt, ct))
                        throw new InvalidOperationException("O profissional já possui consulta nesse horário.");

                    // Paciente só 1 consulta por dia (regra do enunciado)
                    if (await _repository.ExistsPatientAppointmentWithHealthcare(
                        dto.PatientId, dto.HealthcareId, DateOnly.FromDateTime(startAt), ct))
                    {
                        throw new InvalidOperationException("O paciente já possui consulta com este profissional neste dia.");
                    }

                    // Persistência
                    var entity = new Appointment
                    {
                        AppointmentId = Guid.NewGuid(),
                        HealthcareId = dto.HealthcareId,
                        PatientId = dto.PatientId,
                        StartAt = startAt
                    };

                    await _repository.CreateAsync(entity, ct);
                    await uow.CommitAsync(ct);

                    _logger.LogInformation("Consulta {AppointmentId} agendada com sucesso", entity.AppointmentId);

                    return new AppointmentsResponseDto(
                        dto.HealthcareId,
                        dto.PatientId,
                        DateOnly.FromDateTime(startAt),
                        startAt
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falha ao agendar consulta (HealthcareId={HealthcareId}, PatientId={PatientId}, Day={Day}, Hour={Hour})",
                        dto.HealthcareId, dto.PatientId, dto.Day, dto.Hour);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        // ----------------- validações & helpers -----------------

        private static void ValidarJanela(DateTime dataHora)
        {
            var t = TimeOnly.FromDateTime(dataHora);
            if (t < Inicio || t >= FimExclusivo)
                throw new InvalidOperationException("Horário fora da janela de atendimento (08:00–18:00).");
        }

        private static void ValidarDiaUtil(DateTime dataHora)
        {
            var dow = dataHora.DayOfWeek;
            if (dow is DayOfWeek.Saturday or DayOfWeek.Sunday)
                throw new InvalidOperationException("Agendamentos são permitidos apenas de segunda a sexta.");
        }

        private static void ValidarMultiploDe30(DateTime dataHora)
        {
            if (dataHora.Second != 0 || dataHora.Millisecond != 0 || dataHora.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new InvalidOperationException("O horário deve estar alinhado em minutos exatos.");

            var minutes = dataHora.Minute + dataHora.Hour * 60;
            if (minutes % SlotMinutes != 0)
                throw new InvalidOperationException("Consultas têm duração de 30 minutos; escolha um horário em múltiplos de 30 (ex.: 08:00, 08:30…).");
        }

        private static ReadOnlyCollection<DateTime> GerarSlots(DateTime dia)
        {
            var baseDate = dia.Date;
            var inicioDt = baseDate.AddHours(Inicio.Hour).AddMinutes(Inicio.Minute);
            var fimDt = baseDate.AddHours(FimExclusivo.Hour).AddMinutes(FimExclusivo.Minute);

            var list = new List<DateTime>();
            for (var d = inicioDt; d < fimDt; d = d.AddMinutes(SlotMinutes))
                list.Add(d);

            return list.AsReadOnly();
        }

        private static DateTime ToLocalUnspecified(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
                dt = dt.ToLocalTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }
    }
}
