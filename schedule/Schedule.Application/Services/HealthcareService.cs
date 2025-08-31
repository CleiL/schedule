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
            IAppointmentRepository appointments,
            IUserRepository user
        )
        : IHealthcareService
    {
        private readonly ILogger<HealthcareService> _logger = logger;
        private readonly IUnitOfWorkFactory _uowFactory = uow;
        private readonly IHealthcareRepository _repository = repository;
        private readonly IAppointmentRepository _appointments = appointments;
        private readonly IUserRepository _user = user;

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

                    if (await _repository.ExistsByCrmAsync(entity.CRM.Trim(), null, ct))
                        throw new InvalidOperationException("CRM já cadastrado.");

                    if (await _repository.ExistsByEmailAsync(entity.Email.Trim().ToLowerInvariant(), null, ct))
                        throw new InvalidOperationException("Email já cadastrado.");

                    var medico = new Healthcare
                    {
                        HealthcareId = Guid.NewGuid(),
                        Name = entity.Name?.Trim() ?? string.Empty,
                        CRM = entity.CRM?.Trim() ?? string.Empty,
                        Email = entity.Email?.Trim() ?? string.Empty,
                        Speciality = entity.Speciality?.Trim() ?? string.Empty
                    };

                    await _repository.CreateAsync(medico, ct);

                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = entity.Email.Trim().Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(entity.Password.Trim()),
                        Role = "Healthcare",
                        HealthcareId = medico.HealthcareId
                    };

                    await _user.CreateAsync(user, ct);

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

                    var hasAppointments = await _appointments.ExistsPatientAnyAsync(id, ct);
                    if (hasAppointments)
                        throw new InvalidOperationException("Paciente possui consultas e não pode ser excluído.");

                    var user = await _user.GetByHealthcareIdAsync(id, ct);

                    if (user is not null)
                    {
                        await _user.DeleteAsync(user.UserId, ct);
                    }

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

        public async Task<IEnumerable<HealthcareSchedulesResponseDto>> GetAppointmentAsync(CancellationToken ct = default)
        {
            await using var uow = await _uowFactory.CreateAsync(ct);
            await uow.BeginAsync(ct);

            try
            {
                var medicos = await _repository.GetAllAsync(ct);
                var result = new List<HealthcareSchedulesResponseDto>();

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
                        consultas.Select(c => new AppointmentsResponseDto(
                            c.PatientId,
                            c.HealthcareId,
                            c.Date,
                            c.EndAt
                        )).ToList()
                    );

                    result.Add(dto);
                }

                await uow.CommitAsync(ct);
                return result;
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

        public async Task<HealthcareResponseDto> UpdateAsync(HealthcareUpdateDto entity, CancellationToken ct = default)
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
                                 ?? throw new KeyNotFoundException("Medico não encontrado.");

                    // helpers de normalização
                    static string? NullIfWhite(string? s) => string.IsNullOrWhiteSpace(s) ? null : s!.Trim();
                    static string NormalizeEmail(string e) => e.Trim().ToLowerInvariant();
                    static string NormalizeCrmOrThrow(string raw)
                    {
                        var s = raw.Trim().ToUpperInvariant();
                        // Ex.: CRM/SP 123456 (ajuste ao padrão que você desejar)
                        var pattern = @"^CRM/[A-Z]{2}\s\d{1,6}$";
                        if (!Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase))
                            throw new InvalidOperationException("CRM em formato inválido. Ex.: CRM/SP 123456");
                        return s;
                    }

                    // só pega campos enviados (parciais)
                    var nomeNovo = NullIfWhite(entity.Name);
                    var emailNovoRaw = NullIfWhite(entity.Email);
                    var emailNovo = emailNovoRaw is null ? null : NormalizeEmail(emailNovoRaw);
                    var crmNovoRaw = NullIfWhite(entity.CRM);
                    var crmNovo = crmNovoRaw is null ? null : NormalizeCrmOrThrow(crmNovoRaw);
                    var especialidadeNova = NullIfWhite(entity.Speciality);

                    // --- CRM ---
                    if (crmNovo is not null && !crmNovo.Equals(medico.CRM, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await _repository.ExistsByCrmAsync(crmNovo, entity.Id, ct))
                            throw new InvalidOperationException("CRM já cadastrado.");
                        medico.CRM = crmNovo;
                    }

                    // --- E-mail (domínio de Healthcare) + sync com Users ---
                    bool emailMudou = false;
                    var emailAntigo = medico.Email;

                    if (emailNovo is not null && !emailNovo.Equals(medico.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        // unicidade no domínio de profissionais
                        if (await _repository.ExistsByEmailAsync(emailNovo, entity.Id, ct))
                            throw new InvalidOperationException("Email já cadastrado.");

                        medico.Email = emailNovo;
                        emailMudou = true;
                    }

                    // --- Nome/Especialidade (parciais) ---
                    if (nomeNovo is not null) medico.Name = nomeNovo;
                    if (especialidadeNova is not null) medico.Speciality = especialidadeNova;

                    // Persiste o Healthcare
                    await _repository.UpdateAsync(medico, ct);

                    // Sincroniza Users se o e-mail do médico mudou
                    if (emailMudou)
                    {
                        // Pega o user vinculado ao healthcare
                        var user = await _user.GetByHealthcareIdAsync(medico.HealthcareId, ct);
                        if (user is not null)
                        {
                            // garante unicidade no Users (exclui o próprio)
                            if (await _user.ExistsByEmailAsync(medico.Email!, excludeId: user.UserId, ct))
                                throw new InvalidOperationException("Email já cadastrado para login.");

                            user.Email = medico.Email!;
                            user.HealthcareId = medico.HealthcareId; // garante vínculo
                            await _user.UpdateAsync(user, ct);
                        }
                        else
                        {
                            _logger.LogWarning("Médico {HealthcareId} sem usuário vinculado ao atualizar e-mail.", medico.HealthcareId);
                            // Se preferir, crie aqui o user “perdido” — decisão de negócio
                        }
                    }

                    await uow.CommitAsync(ct);
                    _logger.LogInformation("Atualizado médico {HealthcareId}", entity.Id);
                    return medico.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar médico {HealthcareId}", entity.Id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }
    }
}
