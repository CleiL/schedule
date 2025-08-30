using Microsoft.Extensions.Logging;
using Schedule.Application.Dtos.Appointments;
using Schedule.Application.Dtos.Healthcares;
using Schedule.Application.Interfaces;
using Schedule.Application.Mappings;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Text.RegularExpressions;
using System.Threading;
using static Schedule.Application.Dtos.Appointments.AppointmentDto;
using static Schedule.Application.Dtos.Healthcares.HealthcareDto;

namespace Schedule.Application.Services
{
    public class HealthcareService
        (
            ILogger<HealthcareService> logger,
            IUnitOfWorkFactory uow,
            IHealthcareRepository repository,
            IAppointmentRepository appointments
        )
        : IHealthcareService
    {
        private readonly ILogger<HealthcareService> _logger = logger;
        private readonly IUnitOfWorkFactory _uowFactory = uow;
        private readonly IHealthcareRepository _repository = repository;
        private readonly IAppointmentRepository _appointments = appointments;

        public async Task<HealthcareDto.HealthcareResponseDto> CreateAsync(HealthcareDto.HealthcareCreateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Medico.Create"
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var medico = new Healthcare
                    {
                        HealthcareId = Guid.NewGuid(),
                        Name = entity.Name?.Trim() ?? string.Empty,
                        CRM = entity.CRM?.Trim() ?? string.Empty,
                        Email = entity.Email?.Trim() ?? string.Empty,
                        Speciality = entity.Speciality?.Trim() ?? string.Empty
                    };
                    await _repository.CreateAsync(medico, ct);
                    await uow.CommitAsync(ct);
                    _logger.LogInformation("END criação de médico {HealthcareId}", medico.HealthcareId);
                    return medico.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar médico");
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Medico.Delete",
                ["HealthcareId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var result = await _repository.DeleteAsync(id, ct);
                    await uow.CommitAsync(ct);
                    _logger.LogInformation("Excluído médico {HealthcareId}", id);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao excluir médico {HealthcareId}", id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<HealthcareDto.HealthcareResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Medico.GetAll"
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var medicos = await _repository.GetAllAsync(ct);
                    await uow.CommitAsync(ct);
                    return medicos.Select(m => m.ToDto());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao obter médicos");
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<HealthcareDto.HealthcareResponseDto>> GetAppintmentAsync(CancellationToken ct = default)
        {
            await using var uow = await _uowFactory.CreateAsync(ct);
            await uow.BeginAsync(ct);

            try
            {
                var medicos = await _repository.GetAllAsync(ct);
                var result = new List<HealthcareDto.HealthcareSchedulesResponseDto>();

                foreach (var m in medicos)
                {
                    // consulta as consultas do médico
                    var consultas = await _appointments.GetByHealthcareAsync(m.HealthcareId, ct);

                    var dto = new HealthcareSchedulesResponseDto(
                        m.HealthcareId,
                        m.Name ?? string.Empty,
                        m.Email ?? string.Empty,
                        m.CRM ?? string.Empty,
                        m.Speciality ?? string.Empty,
                        consultas.Select(c => new AppointmentsReponseDto(
                            c.PatientId,
                            c.HealthcareId,
                            c.Date,
                            c.EndAt
                        )).ToList()
                    );

                    result.Add(dto);
                }

                await uow.CommitAsync(ct);
                return (IEnumerable<HealthcareResponseDto>)result;
            }
            catch
            {
                await uow.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<HealthcareDto.HealthcareResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Medico.GetById",
                ["HealthcareId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var medico = await _repository.GetByIdAsync(id, ct);
                    await uow.CommitAsync(ct);
                    if (medico == null)
                    {
                        _logger.LogWarning("Médico {HealthcareId} não encontrado", id);
                        return null;
                    }
                    return medico.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao obter médico {MedicoId}", id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<HealthcareDto.HealthcareResponseDto> UpdateAsync(HealthcareDto.HealthcareUpdateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Medico.Update",
                ["HealthcareId"] = entity.Id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var medico = await _repository.GetByIdAsync(entity.Id, ct)
                        ?? throw new KeyNotFoundException("Medico não encontrado");

                    static string? NullIfWhite(string? s) => string.IsNullOrWhiteSpace(s) ? null : s!.Trim();
                    static string? OnlyCrmOrNull(string? s)
                    {
                        if (string.IsNullOrWhiteSpace(s))
                            return null;

                        var pattern = @"^CRM/[A-Z]{2}\s\d{1,6}$";
                        return Regex.IsMatch(s.Trim(), pattern, RegexOptions.IgnoreCase)
                            ? s.Trim().ToUpperInvariant()
                            : null;
                    }

                    var nomeNovo = NullIfWhite(entity.Name);
                    var crmNovo = OnlyCrmOrNull(entity.CRM);
                    var emailNovo = NullIfWhite(entity.Email)?.ToLowerInvariant();
                    var especialidadeNovo = NullIfWhite(entity.Speciality);

                    if (crmNovo is not null && !crmNovo.Equals(medico?.CRM, StringComparison.OrdinalIgnoreCase))
                    {
                        var crmExists = await _repository.ExistsByCrmAsync(crmNovo, entity.Id, ct);
                        if (crmExists)
                            throw new InvalidOperationException("CRM já cadastrado.");
                    }

                    if (emailNovo is not null && !emailNovo.Equals(medico?.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await _repository.ExistsByEmailAsync(emailNovo, entity.Id, ct))
                            throw new InvalidOperationException("Email já cadastrado.");

                        if (medico is not null && emailNovo is not null && !emailNovo.Equals(medico.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            if (await _repository.ExistsByEmailAsync(emailNovo, entity.Id, ct))
                                throw new InvalidOperationException("Email já cadastrado.");
                            medico.Email = emailNovo;
                        }

                    }

                    if (nomeNovo is not null && medico is not null) medico.Name = nomeNovo;
                    if (crmNovo is not null && medico is not null) medico.CRM = crmNovo;
                    if (emailNovo is not null && medico is not null) medico.Email = emailNovo;
                    if (especialidadeNovo is not null && medico is not null) medico.Speciality = especialidadeNovo;

                    if (medico is null)
                        throw new KeyNotFoundException("Medico não encontrado para atualização.");

                    _ = await _repository.UpdateAsync(medico, ct);

                    await uow.CommitAsync(ct);
                    _logger.LogInformation("Atualizado médico {MedicoId}", entity.Id);
                    return medico.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar médico {MedicoId}", entity.Id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }
    }
}
