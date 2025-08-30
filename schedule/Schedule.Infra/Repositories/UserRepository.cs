using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data;

namespace Schedule.Infra.Repositories
{
    public class UserRepository
        (
            ILogger<UserRepository> logger,
            IUnitOfWork uow
        )
        : BaseRepository(uow), IUserRepository
    {
        private readonly ILogger<UserRepository> _logger = logger;

        public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
        {
            if (entity.UserId == Guid.Empty) throw new ArgumentException("UserId inválido.");
            if (string.IsNullOrWhiteSpace(entity.Email)) throw new ArgumentException("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.PasswordHash)) throw new ArgumentException("PasswordHash é obrigatório.");
            if (string.IsNullOrWhiteSpace(entity.Role)) entity.Role = "user";

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "User.Create",
                ["UserId"] = entity.UserId,
                ["Email"] = entity.Email
            }))
            {
                const string sql = """
                    INSERT INTO dbo.Users (UserId, Email, PasswordHash, Role, PatientId, HealthcareId)
                    VALUES (@UserId, @Email, @PasswordHash, @Role, @PatientId, @HealthcareId);
                    """;

                try
                {
                    _logger.LogDebug("Executando INSERT em dbo.Users");

                    var rows = await Conn.ExecuteAsync(new CommandDefinition(
                        sql, entity, transaction: Tx, commandTimeout: 15, commandType: CommandType.Text, cancellationToken: ct));

                    if (rows != 1)
                        throw new DataException($"Insert em dbo.Users afetou {rows} linha(s). Esperado: 1.");

                    _logger.LogInformation("Usuário {Email} registrado com sucesso", entity.Email);
                    return entity;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // UNIQUE/PK
                {
                    _logger.LogWarning(ex, "Conflito de unicidade ao criar usuário {Email}", entity.Email);
                    throw new InvalidOperationException("Já existe um usuário com este e-mail.", ex);
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "User.Delete",
                ["UserId"] = id
            }))
            {
                const string sql = """
                    DELETE FROM dbo.Users
                     WHERE UserId = @id;
                    """;

                var rows = await Conn.ExecuteAsync(new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
                _logger.LogInformation("Excluídos {rows} registro(s)", rows);
                return rows > 0;
            }
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
        {
            const string sql = """
                SELECT COUNT(1)
                  FROM dbo.Users
                 WHERE Email = @Email
                   AND (@ExcludeId IS NULL OR UserId <> @ExcludeId);
                """;

            var count = await Conn.ExecuteScalarAsync<int>(new CommandDefinition(
                sql, new { Email = email, ExcludeId = excludeId }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

            _logger.LogDebug("Existe email {email} (excluindo {excludeId})? {exists}", email, excludeId, count > 0);
            return count > 0;
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT UserId, Email, PasswordHash, Role, PatientId, HealthcareId
                  FROM dbo.Users
                 ORDER BY Email;
                """;

            _logger.LogDebug("Consultando todos os usuários");
            return await Conn.QueryAsync<User>(new CommandDefinition(sql, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            const string sql = """
                SELECT TOP 1 UserId, Email, PasswordHash, Role, PatientId, HealthcareId
                  FROM dbo.Users
                 WHERE Email = @Email;
                """;

            return await Conn.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(sql, new { Email = email }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = """
                SELECT UserId, Email, PasswordHash, Role, PatientId, HealthcareId
                  FROM dbo.Users
                 WHERE UserId = @id;
                """;

            _logger.LogDebug("Consultando usuário por ID {id}", id);
            return await Conn.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(sql, new { id }, transaction: Tx, commandTimeout: 15, cancellationToken: ct));
        }

        public async Task<User> UpdateAsync(User entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "User.Update",
                ["UserId"] = entity.UserId
            }))
            {
                const string sql = """
                    UPDATE dbo.Users
                       SET Email       = @Email,
                           PasswordHash = @PasswordHash,
                           Role         = @Role,
                           PatientId    = @PatientId,
                           HealthcareId = @HealthcareId
                     WHERE UserId      = @UserId;
                    """;

                var rows = await Conn.ExecuteAsync(new CommandDefinition(sql, entity, transaction: Tx, commandTimeout: 15, cancellationToken: ct));

                if (rows != 1)
                    throw new DataException($"Update em dbo.Users afetou {rows} linha(s). Esperado: 1.");

                _logger.LogInformation("Atualizado {rows} registro(s) do usuário {UserId}", rows, entity.UserId);
                return entity;
            }
        }
    }
}
