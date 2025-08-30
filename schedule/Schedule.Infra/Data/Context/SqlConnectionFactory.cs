using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data;

namespace Schedule.Infra.Data.Context
{
    public class SqlConnectionFactory
        : IDbConnectionFactory
    {
        private readonly string _cs;

        public SqlConnectionFactory(IOptions<DbOptions> opts)
        {
            _cs = opts.Value.ConnectionString!;
            if (string.IsNullOrWhiteSpace(_cs))
                throw new InvalidOperationException("Database:ConnectionString não configurada.");
        }

        public Task<IDbConnection> Create(CancellationToken ct = default)
        {
            IDbConnection connection = new SqlConnection(_cs);

            return Task.FromResult(connection);
        }
    }
}
