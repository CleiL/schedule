using Schedule.Core.Entities;

namespace Schedule.Core.Interfaces
{
    public interface IHealthcareRepository
        : IBaseRepository<Healthcare>
    {
        Task<bool> ExistsByCpfAsync(string cpf, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
        Task<Guid?> GetIdByEmailAsync(string email, CancellationToken ct = default);
    }
}
