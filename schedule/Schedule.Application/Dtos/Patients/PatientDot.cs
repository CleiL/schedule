using static Schedule.Application.Dtos.Appointments.AppointmentDto;

namespace Schedule.Application.Dtos.Patients
{
    public static class PatientDot
    {
        public record PatientCreateDto(string Name, string Email, string CPF, string Password);
        public record PatientUpdateDto(Guid Id, string Name, string Email, string CPF);
        public record PatientResponseDto(Guid Id, string Name, string Email, string CPF);
        public record PatientSchedulesResponseDto(Guid Id, string Name, string Email, string CPF, List<AppointmentsReponseDto> Schedulles);
    }
}
