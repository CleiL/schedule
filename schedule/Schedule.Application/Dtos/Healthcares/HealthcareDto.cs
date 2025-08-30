using static Schedule.Application.Dtos.Appointments.AppointmentDto;

namespace Schedule.Application.Dtos.Healthcares
{
    public static class HealthcareDto
    {
        public record HealthcareCreateDto(string Name, string Email, string CRM, string Password, string Speciality);
        public record HealthcareUpdateDto(Guid Id, string Name, string Email, string CRM, string Speciality);
        public record HealthcareResponseDto(Guid Id, string Name, string Email, string CRM, string Speciality);
        public record HealthcareSchedulesResponseDto(Guid Id, string Name, string Email, string CRM, string Speciality, List<AppointmentsReponseDto> Schedulles);

    }
}
