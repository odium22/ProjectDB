using System.ComponentModel.DataAnnotations.Schema;
using ProtectDB.Domain.Common;

namespace ProtectDB.Domain.Entities;

// Сессия входа пользователя (токен доступа).
public class UserSession : Entity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = string.Empty; // уникальный токен сессии
    public string? IpAddress { get; set; }            // адрес входа
    public DateTime CreatedAtUtc { get; set; }        // начало сессии
    public DateTime ExpiresAtUtc { get; set; }        // плановое окончание
    public DateTime? RevokedAtUtc { get; set; }       // досрочное завершение

    // Сессия активна, если не истекла и не отозвана.
    [NotMapped]
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
