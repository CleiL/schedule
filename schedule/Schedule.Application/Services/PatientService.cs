using Microsoft.Extensions.Logging;
using Schedule.Application.Dtos.Patients;
using Schedule.Application.Interfaces;
using Schedule.Application.Mappings;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.Text.RegularExpressions;
using static Schedule.Application.Dtos.Appointments.AppointmentDto;
using static Schedule.Application.Dtos.Patients.PatientDot;

namespace Schedule.Application.Services
{
    public class PatientService
        (
            IPatientRepository repository,
            IUserRepository user,
            IAppointmentRepository _appointments,
            ILogger<PatientService> logger,
            IUnitOfWorkFactory uow
        )
        : IPatientService
    {
        private readonly IPatientRepository _repository = repository;
        private readonly IUserRepository _user = user;
        private readonly IAppointmentRepository _appointments = _appointments;
        private readonly IUnitOfWorkFactory _uowFactory = uow;
        private readonly ILogger<PatientService> _logger = logger;

        public async Task<PatientDot.PatientResponseDto> CreateAsync(PatientDot.PatientCreateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Paciente.Create"
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    if (await _repository.ExistsByCpfAsync(entity.CPF.Trim(), null, ct))
                        throw new InvalidOperationException("CPF já cadastrado.");

                    if (await _repository.ExistsByEmailAsync(entity.Email.Trim().ToLowerInvariant(), null, ct))
                        throw new InvalidOperationException("Email já cadastrado.");

                    var paciente = new Patient
                    {
                        PatientId = Guid.NewGuid(),
                        Name = entity.Name.Trim(),
                        CPF = entity.CPF.Trim(),
                        Email = entity.Email.Trim()
                    };

                    await _repository.CreateAsync(paciente, ct);


                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = entity.Email.Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(entity.Password.Trim()),
                        Role = "Patient",
                        PatientId = paciente.PatientId
                    };

                    await _user.CreateAsync(user, ct);

                    await uow.CommitAsync(ct);
                    _logger.LogInformation("END criação de paciente {PacienteId}", paciente.PatientId);
                    return paciente.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar paciente");
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Paciente.Delete",
                ["PatientId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var hasAppointments = await _appointments.ExistsPatientAnyAsync(id, ct); 
                    if (hasAppointments)
                        throw new InvalidOperationException("Paciente possui consultas e não pode ser excluído.");

                    var user = await _user.GetByPatientIdAsync(id, ct); 

                    if (user is not null)
                    {
                        await _user.DeleteAsync(user.UserId, ct);
                    }

                    var ok = await _repository.DeleteAsync(id, ct);

                    await uow.CommitAsync(ct);
                    return ok;
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<PatientDot.PatientResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Paciente.GetAll"
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var pacientes = await _repository.GetAllAsync(ct);
                    await uow.CommitAsync(ct);
                    _logger.LogInformation("List retornou entidades");
                    return pacientes.Select(p => p.ToDto());
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<PatientSchedulesResponseDto>> GetAppointmentAsync(CancellationToken ct = default)
        {
            await using var uow = await _uowFactory.CreateAsync(ct);
            try
            {
                await uow.BeginAsync(ct);

                // pega todos pacientes com suas consultas
                var pacientes = await _repository.GetAllAsync(ct);
                var result = new List<PatientSchedulesResponseDto>();

                foreach (var paciente in pacientes)
                {
                    var consultas = await _appointments.GetByPatientAsync(paciente.PatientId, ct);

                    var dto = new PatientSchedulesResponseDto
                    (
                        paciente.PatientId,
                        paciente.Name,
                        paciente.Email,
                        paciente.CPF,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar consultas de pacientes");
                await uow.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<PatientResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Paciente.GetById",
                ["PacienteId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var paciente = await _repository.GetByIdAsync(id, ct);
                    await uow.CommitAsync(ct);
                    _logger.LogInformation("GetById({Id}) {Found}", id, paciente is null ? "não encontrado" : "ok");
                    return paciente?.ToDto();
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<PatientResponseDto> UpdateAsync(PatientUpdateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Paciente.Update",
                ["PatientId"] = entity.Id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var paciente = await _repository.GetByIdAsync(entity.Id, ct)
                                   ?? throw new KeyNotFoundException("Paciente não encontrado.");

                    static string? NullIfWhite(string? s) => string.IsNullOrWhiteSpace(s) ? null : s!.Trim();
                    static string? OnlyDigitsOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : Regex.Replace(s!, @"\D", "");

                    var nomeNovo = NullIfWhite(entity.Name);
                    var cpfNovo = OnlyDigitsOrNull(entity.CPF);
                    var emailNovo = NullIfWhite(entity.Email)?.ToLowerInvariant();

                    if (cpfNovo is not null && !cpfNovo.Equals(paciente.CPF, StringComparison.Ordinal))
                    {
                        if (await _repository.ExistsByCpfAsync(cpfNovo, entity.Id, ct))
                            throw new InvalidOperationException("CPF já cadastrado.");
                        paciente.CPF = cpfNovo;
                    }

                    bool emailMudou = false;
                    var emailAntigo = paciente.Email;

                    if (emailNovo is not null && !emailNovo.Equals(paciente.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await _repository.ExistsByEmailAsync(emailNovo, entity.Id, ct))
                            throw new InvalidOperationException("Email já cadastrado para paciente.");

                        paciente.Email = emailNovo;
                        emailMudou = true;
                    }

                    if (nomeNovo is not null) paciente.Name = nomeNovo;

                    await _repository.UpdateAsync(paciente, ct);

                    if (emailMudou)
                    {
                        var user = await _user.GetByEmailAsync(emailAntigo!, ct);

                        if (user is null)
                        {
                            user = await _user.GetByEmailAsync(paciente.Email!, ct);
                        }

                        if (user is not null)
                        {
                            if (await _user.ExistsByEmailAsync(paciente.Email!, excludeId: user.UserId, ct))
                                throw new InvalidOperationException("Email já cadastrado para login.");

                            user.Email = paciente.Email!;
                            user.PatientId = paciente.PatientId;

                            await _user.UpdateAsync(user, ct);
                        }
                    }

                    await uow.CommitAsync(ct);
                    return paciente.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar paciente {PacienteId}", entity.Id);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }
    }
}
