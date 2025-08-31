using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data;

namespace Schedule.Infra.Repositories
{
    public class HealthcareRepository
        (
            ILogger<HealthcareRepository> logger,
            IUnitOfWork uow
        )
        : BaseRepository(uow), IHealthcareRepository
    {
        private readonly ILogger<HealthcareRepository> _logger = logger;
        public async Task<Healthcare> CreateAsync(Healthcare entity, CancellationToken ct = default)
        {
            if (entity.HealthcareId == Guid.Empty) throw new ArgumentException("HealthcareId inválido.");
            if (string.IsNullOrWhiteSpace(entity.Name)) throw new ArgumentException("Name é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.CRM)) throw new ArgumentException("CRM é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.Speciality)) throw new ArgumentException("Speciality é obrigatório.");

            entity.CRM = entity.CRM.Trim();

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Healthcare.Create",
                ["HealthcareId"] = entity.HealthcareId,
                ["CRM"] = entity.CRM
            }))
            {
                const string sql = """
                    INSERT INTO dbo.Healthcare (HealthcareId, Name, Email, CRM, Speciality)
                    VALUES (@HealthcareId, @Name, @Email, @CRM, @Speciality);
                    """;
                try
                {
                    var rows = await Conn.ExecuteAsync(new CommandDefinition(
                        sql, entity, transaction: Tx, commandTimeout: 15, commandType: CommandType.Text, cancellationToken: ct));

                    if (rows != 1)
                        throw new DataException($"Insert em dbo.Healthcare afetou {rows} linha(s). Esperado: 1.");

                    _logger.LogInformation("Profissional {Name} criado com sucesso (CRM {CRM})", entity.Name, entity.CRM);
                    return entity;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // UNIQUE/PK
                {
                    _logger.LogWarning(ex, "Conflito de unicidade ao criar profissional (CRM {CRM})", entity.CRM);
                    throw new InvalidOperationException("Já existe um profissional com este CRM.", ex);
                }
            }
        }

        public async Task<Healthcare> UpdateAsync(Healthcare entity, CancellationToken ct = default)
        {
            if (entity.HealthcareId == Guid.Empty) throw new ArgumentException("HealthcareId inválido.");
            if (string.IsNullOrWhiteSpace(entity.Name)) throw new ArgumentException("Name é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.CRM)) throw new ArgumentException("CRM é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.Speciality)) throw new ArgumentException("Speciality é obrigatório.");

            entity.CRM = entity.CRM.Trim();

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Healthcare.Update",
                ["HealthcareId"] = entity.HealthcareId
            }))
            {
                const string sql = """
                    UPDATE dbo.Healthcare
                       SET Name = @Name,
                           Email = @Email,
                           CRM = @CRM,
                           Speciality = @Speciality
                     WHERE HealthcareId = @HealthcareId;
                    """;
                try
                {
                    var rows = await Conn.ExecuteAsync(new CommandDefinition(
                        sql, entity, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

                    if (rows != 1)
                        throw new DataException($"Update em dbo.Healthcare afetou {rows} linha(s). Esperado: 1.");

                    _logger.LogInformation("Profissional {HealthcareId} atualizado", entity.HealthcareId);
                    return entity;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    _logger.LogWarning(ex, "Conflito de unicidade ao atualizar profissional (CRM {CRM})", entity.CRM);
                    throw new InvalidOperationException("Já existe um profissional com este CRM.", ex);
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Healthcare.Delete",
                ["HealthcareId"] = id
            }))
            {
                const string sql = """
                    DELETE FROM dbo.Healthcare
                     WHERE HealthcareId = @id;
                    """;

                var rows = await Conn.ExecuteAsync(new CommandDefinition(
                    sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

                _logger.LogInformation("Excluídos {rows} registro(s) de dbo.Healthcare", rows);
                return rows > 0;
            }
        }

        public async Task<Healthcare?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = """
                SELECT HealthcareId, Name, Email, CRM, Speciality
                  FROM dbo.Healthcare
                 WHERE HealthcareId = @id;
                """;

            return await Conn.QuerySingleOrDefaultAsync<Healthcare>(
                new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<IEnumerable<Healthcare>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT HealthcareId, Name, Email, CRM, Speciality
                  FROM dbo.Healthcare
                 ORDER BY Name;
                """;

            return await Conn.QueryAsync<Healthcare>(
                new CommandDefinition(sql, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<Guid?> GetIdByEmailAsync(string email, CancellationToken ct = default)
        {
            const string sql = """
                SELECT TOP 1 HealthcareId
                  FROM dbo.Healthcare
                 WHERE Email = @Email;
                """;

            return await Conn.ExecuteScalarAsync<Guid?>(
                new CommandDefinition(sql, new { Email = email }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<bool> ExistsByCrmAsync(string crm, Guid? excludeId = null, CancellationToken ct = default)
        {
            var crmHealthcare = (crm ?? string.Empty).Trim();
            if (crmHealthcare.Length == 0) return false;

            const string sql = """
                SELECT COUNT(1)
                  FROM dbo.Healthcare
                 WHERE CRM = @CRM
                   AND (@ExcludeId IS NULL OR HealthcareId <> @ExcludeId);
                """;

            var count = await Conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { CRM = crm, ExcludeId = excludeId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return count > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
        {
            const string sql = """
                SELECT COUNT(1)
                  FROM dbo.Healthcare
                 WHERE Email = @Email
                   AND (@ExcludeId IS NULL OR HealthcareId <> @ExcludeId);
                """;

            var count = await Conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { Email = email, ExcludeId = excludeId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            return count > 0;
        }

      
    }
}
