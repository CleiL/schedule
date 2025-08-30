namespace Schedule.Application.Dtos.Users
{
    public static class UserDto
    {
        public record UserCreateDto(string Email, string Password, string Role);
        public record UserUpdateDto(Guid UserId,string Email, string Password, string Role);
        public record UserResponseDto(string Email, string Role);
    }
}
