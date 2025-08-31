using Microsoft.Extensions.Logging;
using Schedule.Application.Dtos.Users;
using Schedule.Application.Interfaces;
using Schedule.Application.Mappings;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;

namespace Schedule.Application.Services
{
    public class UserService
        (
            IUserRepository repository,
            IUnitOfWorkFactory uow,
            ILogger<UserService> logger
        )
        : IUserService
    {
        private readonly IUserRepository _repository = repository;
        private readonly IUnitOfWorkFactory _uowFactory = uow;
        private readonly ILogger<UserService> _logger = logger;

        public async Task<UserDto.UserResponseDto> CreateAsync(UserDto.UserCreateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Usuario.Create",
                ["Email"] = entity.Email
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var email = NormalizeEmail(entity.Email);

                    if (await EmailRegistredAsync(email, null, ct))
                        throw new InvalidOperationException("Email já cadastrado.");

                    // Hash da senha (nunca salve plain text)
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(entity.Password);

                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = passwordHash,
                        Role = string.IsNullOrWhiteSpace(entity.Role) ? "User" : entity.Role.Trim()
                    };

                    await _repository.CreateAsync(user, ct);

                    await uow.CommitAsync(ct);
                    _logger.LogInformation("END criação de usuário {UserId}", user.UserId);

                    return user.ToDto();

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar usuário");
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Usuario.Delete",
                ["UsuarioId"] = id
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

        public async Task<IEnumerable<UserDto.UserResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Usuario.GetAll"
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var users = await _repository.GetAllAsync(ct);
                    await uow.CommitAsync(ct);

                    return users.Select(u => new UserDto.UserResponseDto
                    (
                        u.UserId,
                        u.Email,
                        u.Role
                    ));
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<UserDto.UserResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Usuario.GetById",
                ["UsuarioId"] = id
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);
                    var user = await _repository.GetByIdAsync(id, ct);
                    await uow.CommitAsync(ct);

                    return user!.ToDto();
                }
                catch
                {
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }

        public async Task<UserDto.UserResponseDto> UpdateAsync(UserDto.UserUpdateDto entity, CancellationToken ct = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["Flow"] = "Usuario.Update",
                ["UserId"] = entity.UserId
            }))
            {
                await using var uow = await _uowFactory.CreateAsync(ct);
                try
                {
                    await uow.BeginAsync(ct);

                    var user = await _repository.GetByIdAsync(entity.UserId, ct)
                               ?? throw new KeyNotFoundException("Usuário não encontrado.");

                    static string? NullIfWhite(string? s) => string.IsNullOrWhiteSpace(s) ? null : s!.Trim();

                    // campos opcionais no update
                    var emailNovo = NullIfWhite(entity.Email)?.ToLowerInvariant();
                    var roleNova = NullIfWhite(entity.Role);
                    var senhaNova = NullIfWhite(entity.Password);

                    if (emailNovo is not null && !emailNovo.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await EmailRegistredAsync(emailNovo, entity.UserId, ct))
                            throw new InvalidOperationException("Email já cadastrado.");
                        user.Email = emailNovo;
                    }

                    if (roleNova is not null) user.Role = roleNova;

                    if (senhaNova is not null)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(senhaNova);
                    }

                    await _repository.UpdateAsync(user, ct);

                    await uow.CommitAsync(ct);

                    return user.ToDto();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar usuário {UsuarioId}", entity.UserId);
                    await uow.RollbackAsync(ct);
                    throw;
                }
            }
        }
        private static string NormalizeEmail(string email)
           => (email ?? throw new ArgumentNullException(nameof(email))).Trim().ToLowerInvariant();

        private async Task<bool> EmailRegistredAsync(string email, Guid? excludeId, CancellationToken ct)
        {
            var all = await _repository.GetAllAsync(ct);
            return all.Any(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                (excludeId is null || u.UserId != excludeId.Value));
        }
    }
}
