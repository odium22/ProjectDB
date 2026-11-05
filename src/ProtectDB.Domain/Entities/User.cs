using ProtectDB.Domain.Common;

namespace ProtectDB.Domain.Entities;

// Учётная запись пользователя.
public class User : Entity
{
    public string UserName { get; set; } = string.Empty;     // логин, уникальный
    public string DisplayName { get; set; } = string.Empty;  // отображаемое имя
    public string? Email { get; set; }

    public string PasswordHash { get; set; } = string.Empty; // хэш пароля (PBKDF2)
    public string PasswordSalt { get; set; } = string.Empty; // соль для хэша

    public bool IsActive { get; set; } = true;               // активна ли учётная запись
    public bool IsLocked { get; set; }                       // блокировка после неудачных входов
    public int FailedLoginAttempts { get; set; }             // счётчик неудачных входов
    public DateTime? LastLoginAtUtc { get; set; }            // время последнего входа
    public DateTime CreatedAtUtc { get; set; }               // время создания

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
