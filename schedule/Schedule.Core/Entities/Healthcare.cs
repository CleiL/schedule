namespace Schedule.Core.Entities
{
    public class Healthcare
    {
        public Guid HealthcareId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CRM { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;

    }
}
