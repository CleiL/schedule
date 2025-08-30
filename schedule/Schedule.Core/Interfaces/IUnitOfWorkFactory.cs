namespace Schedule.Core.Interfaces
{
    public interface IUnitOfWorkFactory
    {
        Task<IUnitOfWork> CreateAsync(CancellationToken ct = default);
    }
}
