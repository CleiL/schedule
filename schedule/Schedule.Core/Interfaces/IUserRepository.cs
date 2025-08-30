using Schedule.Core.Entities;

namespace Schedule.Core.Interfaces
{
    public interface IUserRepository
        : IBaseRepository<User>
    {
        Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    }
}
