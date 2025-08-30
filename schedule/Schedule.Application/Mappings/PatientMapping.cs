using Schedule.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
               e.CPF,
               e.Email
           );

        public static Patient ToEntity(this PatientCreateDto dto)
            => new Patient
            {
                PatientId = Guid.NewGuid(),
                Name = dto.Name.Trim() ?? string.Empty,
                CPF = dto.CPF.Trim() ?? string.Empty,
                Email = dto.Email.Trim() ?? string.Empty,
            };

        public static void Apply(this Patient entity, PatientUpdateDto dto)
        {
            entity.Name = dto.Name?.Trim() ?? string.Empty;
            entity.CPF = dto.CPF?.Trim() ?? string.Empty;
            entity.Email = dto.Email?.Trim() ?? string.Empty;
        }
    }
}
