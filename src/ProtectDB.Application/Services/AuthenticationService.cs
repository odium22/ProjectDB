using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;
using ProtectDB.Application.Security;
using ProtectDB.Data;
using ProtectDB.Domain.Entities;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Services;

// Аутентификация: проверка пароля, блокировка после неудач, сессии.
public class AuthenticationService : IAuthenticationService
{
    public const int MaxFailedLoginAttempts = 5;                       // порог блокировки
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    private readonly ProtectDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;
    private readonly ICurrentUserContext _currentUser;

    public AuthenticationService(
        ProtectDbContext db, IPasswordHasher hasher, IAuditService audit, ICurrentUserContext currentUser)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<LoginResult> LoginAsync(
        string userName, string password, string? ipAddress = null, CancellationToken ct = default)
    {
        userName = userName.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (user is null)
        {
            await _audit.WriteAsync(AuditAction.LoginFailed, AuditResult.Failure,
                nameof(User), null, "Пользователь не найден", ipAddress, userName: userName, ct: ct);
            return Failed("Неверный логин или пароль.");
        }

        if (!user.IsActive)
        {
            await _audit.WriteAsync(AuditAction.LoginFailed, AuditResult.Failure,
                nameof(User), user.Id.ToString(), "Учётная запись отключена", ipAddress, userId: user.Id, ct: ct);
            return Failed("Учётная запись отключена.");
        }

        if (user.IsLocked)
        {
            await _audit.WriteAsync(AuditAction.LoginFailed, AuditResult.Failure,
                nameof(User), user.Id.ToString(), "Учётная запись заблокирована", ipAddress, userId: user.Id, ct: ct);
            return Failed("Учётная запись заблокирована.");
        }

        if (!_hasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            user.FailedLoginAttempts++;

            // При достижении порога учётная запись блокируется.
            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.IsLocked = true;
            }

            await _db.SaveChangesAsync(ct);
            await _audit.WriteAsync(AuditAction.LoginFailed, AuditResult.Failure,
                nameof(User), user.Id.ToString(),
                $"Неверный пароль. Неудачных попыток: {user.FailedLoginAttempts}", ipAddress, userId: user.Id, ct: ct);

            return Failed("Неверный логин или пароль.");
        }

        // Успешный вход: сброс счётчика, отметка времени, новая сессия.
        user.FailedLoginAttempts = 0;
        user.LastLoginAtUtc = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        var session = new UserSession
        {
            UserId = user.Id,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            IpAddress = ipAddress,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime)
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _currentUser.Set(user.Id, user.UserName);
        await _audit.WriteAsync(AuditAction.Login, AuditResult.Success,
            nameof(User), user.Id.ToString(), "Успешный вход", ipAddress, userId: user.Id, ct: ct);

        return new LoginResult
        {
            Success = true,
            UserId = user.Id,
            UserName = user.UserName,
            Token = session.Token,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }

    public async Task LogoutAsync(string token, CancellationToken ct = default)
    {
        var session = await _db.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Token == token, ct);
        if (session is null || !session.IsActive)
        {
            return;
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Logout, AuditResult.Success,
            nameof(UserSession), session.Id.ToString(), "Выход из системы",
            userId: session.UserId, userName: session.User.UserName, ct: ct);

        _currentUser.Clear();
    }

    public async Task<OperationResult> RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
        {
            return OperationResult.Fail("Сессия не найдена.");
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(UserSession), session.Id.ToString(), "Сессия отозвана", userId: session.UserId, ct: ct);

        return OperationResult.Ok();
    }

    private static LoginResult Failed(string error) => new() { Success = false, Error = error };
}
