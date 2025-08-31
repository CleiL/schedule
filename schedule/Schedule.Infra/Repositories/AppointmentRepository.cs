using Dapper;
using Microsoft.Extensions.Logging;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data;

namespace Schedule.Infra.Repositories
{
    public class AppointmentRepository
        (
            ILogger<AppointmentRepository> logger,
            IUnitOfWork uow
        ) 
        : BaseRepository(uow), IAppointmentRepository
    {
        private readonly ILogger<AppointmentRepository> _logger = logger;

        // overlap de 30 minutos: existe se A.Start < B.End e A.End > B.Start
        private const string OverlapPredicate = "(StartAt < @EndAt) AND (DATEADD(MINUTE,30,StartAt) > @StartAt)";

        public async Task<Appointment> CreateAsync(Appointment entity, CancellationToken ct = default)
        {
            if (entity.AppointmentId == Guid.Empty) throw new ArgumentException("AppointmentId inválido.");
            if (entity.PatientId == Guid.Empty) throw new ArgumentException("PatientId obrigatório.");
            if (entity.HealthcareId == Guid.Empty) throw new ArgumentException("HealthcareId obrigatório.");

            // checagem de conflito para o profissional
            if (await HasHealthcareConflict(entity.HealthcareId, entity.StartAt, ct))
                throw new InvalidOperationException("Profissional já possui consulta nesse horário.");

            // checagem de conflito para o paciente
            if (await HasPatientConflict(entity.PatientId, entity.StartAt, ct))
                throw new InvalidOperationException("Paciente já possui consulta nesse horário.");

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Appointment.Create",
                ["AppointmentId"] = entity.AppointmentId,
                ["HealthcareId"] = entity.HealthcareId,
                ["PatientId"] = entity.PatientId,
                ["StartAt"] = entity.StartAt
            }))
            {
                const string sql = """
                    INSERT INTO dbo.Appointments (AppointmentId, StartAt, PatientId, HealthcareId)
                    VALUES (@AppointmentId, @StartAt, @PatientId, @HealthcareId);
                    """;

                var rows = await Conn.ExecuteAsync(new CommandDefinition(
                    sql, entity, transaction: Tx, commandTimeout: 15, commandType: CommandType.Text, cancellationToken: ct));

                if (rows != 1)
                    throw new DataException($"Insert em dbo.Appointments afetou {rows} linha(s). Esperado: 1.");

                _logger.LogInformation("Consulta criada: {AppointmentId}", entity.AppointmentId);
                return entity;
            }
        }

        public async Task<Appointment> UpdateAsync(Appointment entity, CancellationToken ct = default)
        {
            if (entity.AppointmentId == Guid.Empty) throw new ArgumentException("AppointmentId inválido.");

            // regra: ao reagendar, checar conflito (ignorando a própria consulta)
            var endAt = entity.StartAt.AddMinutes(30);

            const string sqlConflictHealthcare = $"""
                SELECT 1
                  FROM dbo.Appointments
                 WHERE HealthcareId = @HealthcareId
                   AND AppointmentId <> @AppointmentId
                   AND {OverlapPredicate};
                """;

            var hasHCConflict = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sqlConflictHealthcare, new
                {
                    entity.HealthcareId,
                    entity.AppointmentId,
                    StartAt = entity.StartAt,
                    EndAt = endAt
                }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            if (hasHCConflict.HasValue)
                throw new InvalidOperationException("Profissional já possui consulta nesse horário.");

            const string sqlConflictPatient = $"""
                SELECT 1
                  FROM dbo.Appointments
                 WHERE PatientId = @PatientId
                   AND AppointmentId <> @AppointmentId
                   AND {OverlapPredicate};
                """;

            var hasPTConflict = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sqlConflictPatient, new
                {
                    entity.PatientId,
                    entity.AppointmentId,
                    StartAt = entity.StartAt,
                    EndAt = endAt
                }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            if (hasPTConflict.HasValue)
                throw new InvalidOperationException("Paciente já possui consulta nesse horário.");

            const string sql = """
                UPDATE dbo.Appointments
                   SET StartAt     = @StartAt,
                       PatientId    = @PatientId,
                       HealthcareId = @HealthcareId
                 WHERE AppointmentId = @AppointmentId;
                """;

            var rows = await Conn.ExecuteAsync(new CommandDefinition(
                sql, entity, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            if (rows != 1)
                throw new DataException($"Update em dbo.Appointments afetou {rows} linha(s). Esperado: 1.");

            _logger.LogInformation("Consulta {AppointmentId} atualizada", entity.AppointmentId);
            return entity;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = """
                DELETE FROM dbo.Appointments
                 WHERE AppointmentId = @id;
                """;

            var rows = await Conn.ExecuteAsync(new CommandDefinition(
                sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            _logger.LogInformation("Excluídos {rows} registro(s) de dbo.Appointments", rows);
            return rows > 0;
        }

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = """
                SELECT AppointmentId, StartAt,
                       DATEADD(MINUTE,30,StartAt) AS EndAt,
                       PatientId, HealthcareId
                  FROM dbo.Appointments
                 WHERE AppointmentId = @id;
                """;

            return await Conn.QuerySingleOrDefaultAsync<Appointment>(
                new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT AppointmentId, StartAt,
                       DATEADD(MINUTE,30,StartAt) AS EndAt,
                       PatientId, HealthcareId
                  FROM dbo.Appointments
                 ORDER BY StartAt DESC;
                """;

            return await Conn.QueryAsync<Appointment>(new CommandDefinition(
                sql, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<IEnumerable<Appointment>> GetByHealthcareAsync(Guid healthcareId, CancellationToken ct = default)
        {
            const string sql = """
                SELECT AppointmentId, StartAt,
                       DATEADD(MINUTE,30,StartAt) AS EndAt,
                       PatientId, HealthcareId
                  FROM dbo.Appointments
                 WHERE HealthcareId = @HealthcareId
                 ORDER BY StartAt DESC;
                """;

            return await Conn.QueryAsync<Appointment>(new CommandDefinition(
                sql, new { HealthcareId = healthcareId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        /// <summary>
        /// Verifica se o profissional já tem consulta no "instante" informado.
        /// Considera janela de 30min: StartAt <= hour < EndAt.
        /// </summary>
        public async Task<bool> ExistsHealthcareHour(Guid healthcareId, DateTime hour, CancellationToken ct = default)
        {
            const string sql = """
                SELECT 1
                  FROM dbo.Appointments
                 WHERE HealthcareId = @HealthcareId
                   AND @Hour >= StartAt
                   AND @Hour <  DATEADD(MINUTE,30,StartAt);
                """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { HealthcareId = healthcareId, Hour = hour }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return exists.HasValue;
        }

        /// <summary>
        /// Verifica se o paciente possui alguma consulta no DIA informado (independente do horário).
        /// Usa a coluna persistida [Date].
        /// </summary>
        public async Task<bool> ExistsPatientAppointment(Guid patientId, DateOnly day, CancellationToken ct = default)
        {
            const string sql = """
                SELECT 1
                  FROM dbo.Appointments
                 WHERE PatientId = @PatientId
                   AND [Date] = @Day;
                """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { PatientId = patientId, Day = day.ToDateTime(TimeOnly.MinValue).Date }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return exists.HasValue;
        }

        /// <summary>
        /// Retorna os horários (StartAt) futuros ocupados do profissional.
        /// Obs: sua assinatura não recebe "dia"; então retorno a partir de agora.
        /// Ajuste se quiser filtrar por dia específico.
        /// </summary>
        public async Task<IEnumerable<DateTime>> GetProfessionalBusySchedules(Guid healthcareId, DateOnly day, CancellationToken ct = default)
        {
            var dayStart = day.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            const string sql = """
            SELECT StartAt
              FROM dbo.Appointments
             WHERE HealthcareId = @HealthcareId
               AND StartAt >= @DayStart
               AND StartAt <  @DayEnd
             ORDER BY StartAt;
            """;

            return await Conn.QueryAsync<DateTime>(
                new CommandDefinition(sql,
                    new { HealthcareId = healthcareId, DayStart = dayStart, DayEnd = dayEnd },
                    transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        // --------- helpers privados ---------

        private async Task<bool> HasHealthcareConflict(Guid healthcareId, DateTime startAt, CancellationToken ct)
        {
            var endAt = startAt.AddMinutes(30);

            var sql = $"""
                SELECT 1
                  FROM dbo.Appointments
                 WHERE HealthcareId = @HealthcareId
                   AND {OverlapPredicate};
                """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { HealthcareId = healthcareId, StartAt = startAt, EndAt = endAt },
                                      transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return exists.HasValue;
        }

        private async Task<bool> HasPatientConflict(Guid patientId, DateTime startAt, CancellationToken ct)
        {
            var endAt = startAt.AddMinutes(30);

            var sql = $"""
                SELECT 1
                  FROM dbo.Appointments
                 WHERE PatientId = @PatientId
                   AND {OverlapPredicate};
                """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { PatientId = patientId, StartAt = startAt, EndAt = endAt },
                                      transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return exists.HasValue;
        }

        public async Task<bool> ExistsPatientAppointmentWithHealthcare(Guid patientId, Guid healthcareId, DateOnly day, CancellationToken ct = default)
        {
            var dayStart = day.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            const string sql = """
            SELECT 1
              FROM dbo.Appointments
             WHERE PatientId    = @PatientId
               AND HealthcareId = @HealthcareId
               AND StartAt >= @DayStart
               AND StartAt <  @DayEnd;
            """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql,
                    new { PatientId = patientId, HealthcareId = healthcareId, DayStart = dayStart, DayEnd = dayEnd },
                    transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return exists.HasValue;
        }

        public async Task<bool> ExistsPatientAnyAsync(Guid patientId, CancellationToken ct = default)
        {
            const string sql = """
                SELECT 1
                  FROM dbo.Appointments
                 WHERE PatientId = @PatientId
            """;

            var exists = await Conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { PatientId = patientId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct)
            );

            return exists.HasValue;
        }

        public async Task<IEnumerable<Appointment>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
        {
            const string sql = """
                SELECT AppointmentId, StartAt,
                       DATEADD(MINUTE,30,StartAt) AS EndAt,
                       PatientId, HealthcareId
                  FROM dbo.Appointments
                 WHERE PatientId = @patientId
                 ORDER BY StartAt DESC;
                """;

            return await Conn.QueryAsync<Appointment>(new CommandDefinition(
                sql, new { PatientId = patientId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }
    }
}
