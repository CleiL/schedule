using Dapper;
using Microsoft.Extensions.Logging;
using Schedule.Core.Interfaces;
using System.Reflection;

namespace Schedule.Infra.Data
{
    public class SchemaInitializer(
        IDbConnectionFactory factory,
        ILogger<SchemaInitializer> logger)
        : ISchemaInitializer
    {
        private readonly IDbConnectionFactory _factory = factory;
        private readonly ILogger<SchemaInitializer> _logger = logger;

        public async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            var conn = await _factory.Create(ct);
            var tx = conn.BeginTransaction();

            try
            {
                var sql = ReadEmbedded("Schedule.Infra.Data.Scripts.schema.sql");
                await conn.ExecuteAsync(sql, transaction: tx);
                tx.Commit();

                _logger.LogInformation("Schema aplicado com sucesso!");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Erro ao aplicar schema.");
                throw;
            }
        }

        private static string ReadEmbedded(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Recurso {resourceName} não encontrado.");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
