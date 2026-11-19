using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;
using ProtectDB.Application.Security;
using ProtectDB.Data;
using ProtectDB.Domain.Entities;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Services;

// Управление учётными записями. Каждое изменение фиксируется в аудите.
public class UserService : IUserService
{
    private readonly ProtectDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public UserService(ProtectDbContext db, IPasswordHasher hasher, IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                LastLoginAtUtc = u.LastLoginAtUtc,
                CreatedAtUtc = u.CreatedAtUtc,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList()
            })
            .ToListAsync(ct);

    public Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                LastLoginAtUtc = u.LastLoginAtUtc,
                CreatedAtUtc = u.CreatedAtUtc,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList()
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<int>> CreateAsync(
        string userName, string displayName, string password, string? email = null, CancellationToken ct = default)
    {
        userName = userName.Trim();
        displayName = displayName.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(displayName))
        {
            return OperationResult<int>.Fail("Логин и отображаемое имя обязательны.");
        }

        if (await _db.Users.AnyAsync(u => u.UserName == userName, ct))
        {
            await _audit.WriteAsync(AuditAction.Create, AuditResult.Failure,
                nameof(User), null, $"Пользователь {userName} уже существует", ct: ct);
            return OperationResult<int>.Fail("Пользователь с таким логином уже существует.");
        }

        var (hash, salt) = _hasher.HashPassword(password);
        var user = new User
        {
            UserName = userName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Create, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Создан пользователь {user.UserName}", ct: ct);

        return OperationResult<int>.Ok(user.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, string displayName, string? email, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        user.DisplayName = displayName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Изменён профиль {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ChangePasswordAsync(int id, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        var (hash, salt) = _hasher.HashPassword(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Сменён пароль {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Учётная запись {user.UserName}: активна={isActive}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> LockAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        user.IsLocked = true;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Lock, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Заблокирован {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> UnlockAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Unlock, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Разблокирован {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> AssignRoleAsync(int userId, string roleName, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        // Повторная выдача игнорируется.
        if (await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, ct))
        {
            return OperationResult.Ok();
        }

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, AssignedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.GrantRole, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Роль {role.Name} выдана {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoveRoleAsync(int userId, string roleName, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return OperationResult.Fail("Пользователь не найден.");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        var link = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, ct);
        if (link is null)
        {
            return OperationResult.Fail("У пользователя нет этой роли.");
        }

        _db.UserRoles.Remove(link);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.RevokeRole, AuditResult.Success,
            nameof(User), user.Id.ToString(), $"Роль {role.Name} снята с {user.UserName}", ct: ct);

        return OperationResult.Ok();
    }
}
