using System.Data;

namespace Schedule.Core.Interfaces
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> Create(CancellationToken ct = default);

    }
}
