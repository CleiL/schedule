using Schedule.Core.Entities;
using static Schedule.Application.Dtos.Users.UserDto;

namespace Schedule.Application.Mappings
{
    public static class UserMapping
    {
        public static UserResponseDto ToDto(this User e)
                   => new UserResponseDto
                   (
                       e.Email,
                       e.Role
                   );

        public static User ToEntity(this UserCreateDto dto)
            => new User
            {
                UserId = Guid.NewGuid(),
                Email = dto.Email?.Trim() ?? string.Empty,
                PasswordHash = dto.Password?.Trim() ?? string.Empty,
                Role = dto.Role?.Trim() ?? string.Empty
            };
    }
}
