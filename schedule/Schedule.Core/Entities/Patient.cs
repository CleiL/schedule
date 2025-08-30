namespace Schedule.Core.Entities
{
    public class Patient
    {
        public Guid PatientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
    }
}
