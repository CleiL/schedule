using static Schedule.Application.Dtos.Users.UserDto;

namespace Schedule.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateAsync(UserCreateDto entity, CancellationToken ct = default);
        Task<UserResponseDto> UpdateAsync(UserUpdateDto entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<UserResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<UserResponseDto>> GetAllAsync(CancellationToken ct = default);
    }
}
