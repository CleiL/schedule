using Schedule.Core.Entities;
using static Schedule.Application.Dtos.Patients.PatientDot;

namespace Schedule.Application.Mappings
{
    public static class PatientMapping
    {
        public static PatientResponseDto ToDto(this Patient e)
           => new PatientResponseDto
           (
               e.PatientId,
               e.Name,
               e.Email,
               e.CPF
           );

        public static Patient ToEntity(this PatientCreateDto dto)
            => new Patient
            {
                PatientId = Guid.NewGuid(),
                Name = dto.Name.Trim() ?? string.Empty,
                Email = dto.Email.Trim() ?? string.Empty,
                CPF = dto.CPF.Trim() ?? string.Empty,
            };

        public static void Apply(this Patient entity, PatientUpdateDto dto)
        {
            entity.Name = dto.Name?.Trim() ?? string.Empty;
            entity.Email = dto.Email?.Trim() ?? string.Empty;
            entity.CPF = dto.CPF?.Trim() ?? string.Empty;
        }
    }
}
