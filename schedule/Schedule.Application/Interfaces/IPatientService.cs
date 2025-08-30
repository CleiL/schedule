using static Schedule.Application.Dtos.Patients.PatientDot;

namespace Schedule.Application.Interfaces
{
    public interface IPatientService
    {
        Task<PatientResponseDto> CreateAsync(PatientCreateDto entity, CancellationToken ct = default);
        Task<PatientResponseDto> UpdateAsync(PatientUpdateDto entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<PatientResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<PatientResponseDto>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<PatientResponseDto>> GetAppointmentAsync(CancellationToken ct = default);
    }
}
