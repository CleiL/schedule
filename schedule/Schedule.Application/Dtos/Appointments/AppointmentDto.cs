namespace Schedule.Application.Dtos.Appointments
{
    public static class AppointmentDto
    {
        public record SpecialityDto(string? Name);
        public record HealthcareListItemDto(Guid HealthcareId, string? Name, string? Specialty);
        public record SlotsRequestDto(Guid HealthcareId, DateOnly Day);
        public record SlotsResponseDto(DateOnly Day, Guid HealthcareId, IReadOnlyList<ScheduleSltoDto> Slots);
        public record AppointmentCreateDto(Guid HealthcareId, Guid PatientId, DateOnly Day, DateTime Hour);
        public record AppointmentUpdateDto(Guid AppointmentId, Guid HealthcareId, Guid PatientId, DateOnly Day, DateTime Hour);
        public record AppointmentsResponseDto(Guid HealthcareId, Guid PatientId, DateOnly Day, DateTime Hour);
        public record ScheduleListItemDto(Guid AppointmentId, DateOnly Day, DateTime Hour, string? HealthcareName, string? PatientName);
        public record ScheduleSltoDto(DateTime Hour, bool Available);
    }
}
