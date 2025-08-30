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
            IAppointmentRepository _appointments,
            ILogger<PatientService> logger,
            IUnitOfWorkFactory uow
        )
        : IPatientService
    {
        private readonly IPatientRepository _repository = repository;
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

        public async Task<IEnumerable<PatientDot.PatientResponseDto>> GetAppointmentAsync(CancellationToken ct = default)
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
                    var consultas = await _appointments.GetByHealthcareAsync(paciente.PatientId, ct);

                    var dto = new PatientSchedulesResponseDto
                    (
                        paciente.PatientId,
                        paciente.Name!,
                        paciente.Email!,
                        paciente.CPF!,
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
                return (IEnumerable<PatientResponseDto>)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar consultas de pacientes");
                await uow.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<PatientDot.PatientResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
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

        public async Task<PatientDot.PatientResponseDto> UpdateAsync(PatientDot.PatientUpdateDto entity, CancellationToken ct = default)
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

                    if (emailNovo is not null && !emailNovo.Equals(paciente.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await _repository.ExistsByEmailAsync(emailNovo, entity.Id, ct))
                            throw new InvalidOperationException("Email já cadastrado.");
                        paciente.Email = emailNovo;
                    }

                    if (nomeNovo is not null) paciente.Name = nomeNovo;
                    if (emailNovo is not null) paciente.Email = emailNovo;

                    await _repository.UpdateAsync(paciente, ct);

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
