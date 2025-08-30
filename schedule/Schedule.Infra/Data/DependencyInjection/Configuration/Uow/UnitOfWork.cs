using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Data.Common;

namespace Schedule.Infra.Data.DependencyInjection.Configuration.Uow
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly string _cs;
        private readonly ILogger<UnitOfWork> _logger;

        private DbConnection? _conn;
        public DbConnection Connection => _conn ??= new SqlConnection(_cs);
        public DbTransaction? Transaction { get; private set; }
        public bool IsActive => Transaction is not null;

        public UnitOfWork(IOptions<DbOptions> opts, ILogger<UnitOfWork> logger)
        {
            _cs = opts.Value.ConnectionString
                ?? throw new InvalidOperationException("Database:ConnectionString não configurada.");
            _logger = logger;
        }

        public async Task BeginAsync(CancellationToken ct = default)
        {
            if (IsActive) return;
            if (Connection.State != System.Data.ConnectionState.Open)
                await Connection.OpenAsync(ct);
            Transaction = await Connection.BeginTransactionAsync(ct);
            _logger.LogDebug("UoW Begin: transação iniciada");
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (!IsActive) throw new InvalidOperationException("Nenhuma transação ativa para Commit.");
            await Transaction!.CommitAsync(ct);
            await Transaction.DisposeAsync();
            Transaction = null;
            _logger.LogDebug("UoW Commit: transação confirmada");
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (!IsActive) return; 
            await Transaction!.RollbackAsync(ct);
            await Transaction.DisposeAsync();
            Transaction = null;
            _logger.LogWarning("UoW Rollback: transação revertida");
        }

        public void Dispose()
        {
            Transaction?.Dispose();
            if (_conn is not null)
            {
                if (_conn.State != System.Data.ConnectionState.Closed) _conn.Close();
                _conn.Dispose();
                _logger.LogDebug("UoW Dispose: conexão fechada");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Transaction is not null) await Transaction.DisposeAsync();
            if (_conn is not null)
            {
                if (_conn.State != System.Data.ConnectionState.Closed) await _conn.CloseAsync();
                await _conn.DisposeAsync();
                _logger.LogDebug("UoW DisposeAsync: conexão fechada");
            }
        }
    }
}
