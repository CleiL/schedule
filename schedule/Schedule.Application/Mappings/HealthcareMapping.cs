using Schedule.Core.Entities;
using static Schedule.Application.Dtos.Healthcares.HealthcareDto;

namespace Schedule.Application.Mappings
{
    public static class HealthcareMapping
    {
        public static HealthcareResponseDto ToDto(this Healthcare e)
           => new HealthcareResponseDto
           (
               e.HealthcareId,
               e.Name,
               e.CRM,
               e.Email,
               e.Speciality
           );

        public static Healthcare ToEntity(this HealthcareCreateDto dto)
            => new Healthcare
            {
                HealthcareId = Guid.NewGuid(),
                Name = dto.Name?.Trim() ?? string.Empty,
                CRM = dto.CRM?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                Speciality = dto.Speciality?.Trim() ?? string.Empty
            };
    }
}
