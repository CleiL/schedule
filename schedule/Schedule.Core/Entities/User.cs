namespace Schedule.Core.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? PatientId { get; set; }
        public Guid? HealthcareId { get; set; }
    }
}
