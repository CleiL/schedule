using Dapper;
using Schedule.Core.Interfaces;
using System.Data;
using System.Data.Common;

namespace Schedule.Infra.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly IUnitOfWork Uow;
        protected BaseRepository(IUnitOfWork uow) => Uow = uow;
        protected DbConnection Conn => Uow.Connection;
        protected DbTransaction? Tx => Uow.Transaction;

        // Blindagem
        protected async Task EnsureOpenAsync(CancellationToken ct)
        {
            if (Conn.State != ConnectionState.Open)
                await Conn.OpenAsync(ct);
        }

        // Execute (INSERT/UPDATE/DELETE)
        protected Task<int> ExecuteAsync(string sql, object? p = null, int? timeout = null, CommandType? type = null, CancellationToken ct = default) =>
            Conn.ExecuteAsync(new CommandDefinition(sql, p, Tx, timeout, type, cancellationToken: ct));

        // Query lista
        protected Task<IEnumerable<T>> QueryAsync<T>(string sql, object? p = null, int? timeout = null, CommandType? type = null, CancellationToken ct = default) =>
            Conn.QueryAsync<T>(new CommandDefinition(sql, p, Tx, timeout, type, cancellationToken: ct));

      
        // Query único (ou default)
        protected Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? p = null, int? timeout = null, CommandType? type = null, CancellationToken ct = default) =>
            Conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, p, Tx, timeout, type, cancellationToken: ct));

        // Query Single (ou default)
        protected Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? p = null, int? timeout = null, CommandType? type = null, CancellationToken ct = default) =>
            Conn.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, p, Tx, timeout, type, cancellationToken: ct));

        // Scalar
        protected Task<T> ExecuteScalarAsync<T>(string sql, object? p = null, int? timeout = null, CommandType? type = null, CancellationToken ct = default) =>
            Conn.ExecuteScalarAsync<T>(new CommandDefinition(sql, p, Tx, timeout, type, cancellationToken: ct));
    }
}
