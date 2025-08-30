using static Schedule.Application.Dtos.Authenticator.AuthDto;

namespace Schedule.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginDto dto, CancellationToken ct = default);
        Task<bool> RegisterPatientAsync(RegisterPatientDto dto, CancellationToken ct = default);
        Task<bool> RegisterHealthcareAsync(RegisterHealthcareDto dto, CancellationToken ct = default);
        Task ConfirmRegisterAsync(RegisterResponseDto dto, CancellationToken ct = default);
    }
}
