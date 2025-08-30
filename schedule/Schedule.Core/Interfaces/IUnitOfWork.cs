using System.Data.Common;

namespace Schedule.Core.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable, IDisposable
    {
        DbConnection Connection { get; }
        DbTransaction? Transaction { get; }
        bool IsActive { get; }
        Task BeginAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}
