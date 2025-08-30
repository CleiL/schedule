namespace Schedule.Core.Entities
{
    public class Appointment
    {
        public Guid AppointmentId { get; set; }
        public DateTime StartAt { get; set; }
        public DateOnly Date => DateOnly.FromDateTime(StartAt);
        public DateTime EndAt => StartAt.AddMinutes(30);
        public Guid PatientId { get; set; }
        public Guid HealthcareId { get; set; }

    }
}
