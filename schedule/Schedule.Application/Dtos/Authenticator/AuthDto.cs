namespace Schedule.Application.Dtos.Authenticator
{
    public static class AuthDto
    {
        public record LoginDto (string Email, string Password);
        public record LoginResponseDto (string Token, string Email, string Role, Guid UserId, Guid PatientId, Guid HealthcareId);
        public record RegisterDto (string Name, string Email, string Password);
        public record RegisterPatientDto (string Name, string Email, string CPF, string Password);
        public record RegisterHealthcareDto (string Name, string Email, string CRM, string Password, string Speciality);
        public record RegisterResponseDto(string Token, string Name);

    }
}
