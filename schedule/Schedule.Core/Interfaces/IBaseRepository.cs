namespace Schedule.Core.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> CreateAsync(T entity, CancellationToken ct = default);
        Task<T> UpdateAsync(T entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    }
}
