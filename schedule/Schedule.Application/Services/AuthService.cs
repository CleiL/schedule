using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Schedule.Application.Interfaces;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using static Schedule.Application.Dtos.Authenticator.AuthDto;

namespace Schedule.Application.Services
{
    public class AuthService
        (
            IUserRepository users,
            IPatientRepository patients,
            IHealthcareRepository healthcare,
            IUnitOfWorkFactory uowFactory,
            IOptions<JWTOptions> jwtOpts,
            ILogger<AuthService> logger
        )
        : IAuthService
    {
        private readonly IUserRepository _users = users;
        private readonly IPatientRepository _patients = patients;
        private readonly IHealthcareRepository _healthcare = healthcare;
        private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
        private readonly JWTOptions _jwt = jwtOpts.Value;
        private readonly ILogger<AuthService> _logger = logger;

        public async Task<LoginResponseDto> AuthenticateAsync(LoginDto dto, CancellationToken ct = default)
        {
            await using var uow = await _uowFactory.CreateAsync(ct);
            try
            {
                await uow.BeginAsync(ct);

                var user = await _users.GetByEmailAsync(dto.Email, ct)
                           ?? throw new InvalidOperationException("Usuário/senha inválidos.");

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    throw new InvalidOperationException("Usuário/senha inválidos.");

                Guid? pacienteId = null;
                Guid? medicoId = null;
                if (string.Equals(user.Role, "Paciente", StringComparison.OrdinalIgnoreCase))
                    pacienteId = await _patients.GetIdByEmailAsync(dto.Email, ct);
                else if (string.Equals(user.Role, "Medico", StringComparison.OrdinalIgnoreCase))
                    medicoId = await _healthcare.GetIdByEmailAsync(dto.Email, ct);

                var token = GenerateToken(user);

                await uow.CommitAsync(ct);
                _logger.LogInformation("Login bem-sucedido para {Email}",dto.Email);

                return new LoginResponseDto
                (
                    token,
                    user.Email!,
                    user.Role!,
                    user.UserId,
                    PatientId: (Guid)user.PatientId!,
                    HealthcareId: (Guid)user.HealthcareId!
                );
            }
            catch
            {
                await uow.RollbackAsync(ct);
                throw;
            }
        }

        public  Task ConfirmRegisterAsync(RegisterResponseDto dto, CancellationToken ct = default)
        {
            _logger.LogInformation("ConfirmRegister: {Nome}", dto.Name);
            return Task.CompletedTask;
        }

        public async Task<bool> RegisterHealthcareAsync(RegisterHealthcareDto dto, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Auth.RegisterMedico",
                ["Email"] = dto.Email,
                ["CRM"] = dto.CRM
            }))
            {
                await using var uow = await _uowFactory.CreateAsync();
                try
                {
                    await uow.BeginAsync();

                    var email = NormalizeEmail(dto.Email!);

                    var existingUsers = await _users.GetAllAsync();
                    if (existingUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("Email já cadastrado para login.");

                    var medico = new Healthcare
                    {
                        HealthcareId = Guid.NewGuid(),
                        Name = dto.Name!.Trim(),
                        Email = email,
                        Speciality = dto.Speciality!,
                        CRM = dto.CRM!
                    };

                    await _healthcare.CreateAsync(medico);

                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password!),
                        Role = "Medico"
                    };
                    await _users.CreateAsync(user);

                    await uow.CommitAsync();
                    _logger.LogInformation("Usuário médico {UserId} registrado", user.UserId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no registro de médico");
                    await uow.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> RegisterPatientAsync(RegisterPatientDto dto, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Auth.RegisterPaciente",
                ["Email"] = dto.Email,
                ["CPF"] = dto.CPF
            }))
            {
                await using var uow = await _uowFactory.CreateAsync();
                try
                {
                    await uow.BeginAsync();

                    // unicidades
                    var email = NormalizeEmail(dto.Email);
                    if (await _patients.ExistsByEmailAsync(email, null))
                        throw new InvalidOperationException("Email já cadastrado para paciente.");
                    if (await _patients.ExistsByCpfAsync(dto.CPF.Trim(), null))
                        throw new InvalidOperationException("CPF já cadastrado para paciente.");

                    // evita e-mail duplicado em USUÁRIOS
                    var existingUsers = await _users.GetAllAsync();
                    if (existingUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("Email já cadastrado para login.");

                    // cria Paciente
                    var paciente = new Patient
                    {
                        PatientId = Guid.NewGuid(),
                        Name = dto.Name.Trim(),
                        CPF = dto.CPF.Trim(),
                        Email = email
                    };
                    await _patients.CreateAsync(paciente);

                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password!),
                        Role = "Paciente"
                    };
                    await _users.CreateAsync(user);

                    await uow.CommitAsync();
                    _logger.LogInformation("Paciente {PatientID} e Usuario {UsuarioId} registrados", paciente.PatientId, user.UserId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no registro de paciente");
                    await uow.RollbackAsync();
                    throw;
                }
            }
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, user.Role ?? "user"),
            new("uid", user.UserId.ToString())
        };
            if (user.PatientId.HasValue) claims.Add(new Claim("pid", user.PatientId.Value.ToString()));
            if (user.HealthcareId.HasValue) claims.Add(new Claim("hid", user.HealthcareId.Value.ToString()));

            var jwt = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string NormalizeEmail(string email)
        => (email ?? throw new ArgumentNullException(nameof(email))).Trim().ToLowerInvariant();
    }
}
