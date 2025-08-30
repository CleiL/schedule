using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data;

namespace Schedule.Infra.Repositories
{
    public class PatientRepository
        (
            ILogger<PatientRepository> logger,
            IUnitOfWork uow
        )
        : BaseRepository(uow), IPatientRepository
    {
        private readonly ILogger<PatientRepository> _logger = logger;

        public async Task<Patient> CreateAsync(Patient entity, CancellationToken ct = default)
        {
            if (entity.PatientId == Guid.Empty) throw new ArgumentException("PatientId inválido.");
            if (string.IsNullOrWhiteSpace(entity.Name)) throw new ArgumentException("Name é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.CPF)) throw new ArgumentException("CPF é obrigatório.");

            // normaliza CPF (só dígitos)
            entity.CPF = new string(entity.CPF.Where(char.IsDigit).ToArray());
            if (entity.CPF.Length != 11) throw new ArgumentException("CPF inválido (esperado 11 dígitos).");

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Patient.Create",
                ["PatientId"] = entity.PatientId,
                ["CPF"] = entity.CPF
            }))
            {
                const string sql = """
                    INSERT INTO dbo.Patients (PatientId, Name, Email, CPF)
                    VALUES (@PatientId, @Name, @Email, @CPF);
                    """;

                try
                {
                    var rows = await Conn.ExecuteAsync(new CommandDefinition(
                        sql, entity, transaction: Tx, commandTimeout: 15, commandType: CommandType.Text, cancellationToken: ct));

                    if (rows != 1)
                        throw new DataException($"Insert em dbo.Patients afetou {rows} linha(s). Esperado: 1.");

                    _logger.LogInformation("Paciente {Name} criado com sucesso (CPF {CPF})", entity.Name, entity.CPF);
                    return entity;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // UNIQUE/PK
                {
                    _logger.LogWarning(ex, "Conflito de unicidade ao criar paciente (CPF {CPF})", entity.CPF);
                    throw new InvalidOperationException("Já existe um paciente com este CPF.", ex);
                }
            }
        }

        public async Task<Patient> UpdateAsync(Patient entity, CancellationToken ct = default)
        {
            if (entity.PatientId == Guid.Empty) throw new ArgumentException("PatientId inválido.");
            if (string.IsNullOrWhiteSpace(entity.Name)) throw new ArgumentException("Name é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.CPF)) throw new ArgumentException("CPF é obrigatório.");

            entity.CPF = new string(entity.CPF.Where(char.IsDigit).ToArray());
            if (entity.CPF.Length != 11) throw new ArgumentException("CPF inválido (esperado 11 dígitos).");

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Patient.Update",
                ["PatientId"] = entity.PatientId
            }))
            {
                const string sql = """
                    UPDATE dbo.Patients
                       SET Name  = @Name,
                           Email = @Email,
                           CPF   = @CPF
                     WHERE PatientId = @PatientId;
                    """;

                try
                {
                    var rows = await Conn.ExecuteAsync(new CommandDefinition(sql, entity, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
                    if (rows != 1)
                        throw new DataException($"Update em dbo.Patients afetou {rows} linha(s). Esperado: 1.");

                    _logger.LogInformation("Paciente {PatientId} atualizado", entity.PatientId);
                    return entity;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // UNIQUE
                {
                    _logger.LogWarning(ex, "Conflito de unicidade ao atualizar paciente (CPF {CPF})", entity.CPF);
                    throw new InvalidOperationException("Já existe um paciente com este CPF.", ex);
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Patient.Delete",
                ["PatientId"] = id
            }))
            {
                const string sql = """
                    DELETE FROM dbo.Patients
                     WHERE PatientId = @id;
                    """;

                var rows = await Conn.ExecuteAsync(new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
                _logger.LogInformation("Excluídos {rows} registro(s) de dbo.Patients", rows);
                return rows > 0;
            }
        }

        public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = """
                SELECT PatientId, Name, Email, CPF
                  FROM dbo.Patients
                 WHERE PatientId = @id;
                """;

            return await Conn.QuerySingleOrDefaultAsync<Patient>(
                new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<IEnumerable<Patient>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT PatientId, Name, Email, CPF
                  FROM dbo.Patients
                 ORDER BY Name;
                """;

            return await Conn.QueryAsync<Patient>(new CommandDefinition(sql, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<Guid?> GetIdByEmailAsync(string email, CancellationToken ct = default)
        {
            const string sql = """
                SELECT TOP 1 PatientId
                  FROM dbo.Patients
                 WHERE Email = @Email;
                """;

            return await Conn.ExecuteScalarAsync<Guid?>(
                new CommandDefinition(sql, new { Email = email }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<bool> ExistsByCpfAsync(string cpf, Guid? excludeId = null, CancellationToken ct = default)
        {
            cpf = new string((cpf ?? "").Where(char.IsDigit).ToArray());
            if (cpf.Length != 11) return false; // evita query inútil

            const string sql = """
                SELECT COUNT(1)
                  FROM dbo.Patients
                 WHERE CPF = @CPF
                   AND (@ExcludeId IS NULL OR PatientId <> @ExcludeId);
                """;

            var count = await Conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { CPF = cpf, ExcludeId = excludeId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return count > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
        {
            const string sql = """
                SELECT COUNT(1)
                  FROM dbo.Patients
                 WHERE Email = @Email
                   AND (@ExcludeId IS NULL OR PatientId <> @ExcludeId);
                """;

            var count = await Conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { Email = email, ExcludeId = excludeId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return count > 0;
        }
    }
}
