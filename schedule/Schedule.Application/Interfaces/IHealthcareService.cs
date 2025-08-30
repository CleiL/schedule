using static Schedule.Application.Dtos.Healthcares.HealthcareDto;

namespace Schedule.Application.Interfaces
{
    public interface IHealthcareService
    {
        Task<HealthcareResponseDto> CreateAsync(HealthcareCreateDto entity, CancellationToken ct = default);
        Task<HealthcareResponseDto> UpdateAsync(HealthcareUpdateDto entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<HealthcareResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<HealthcareResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<HealthcareResponseDto>> GetAppintmentAsync(CancellationToken ct = default);
    }
}
