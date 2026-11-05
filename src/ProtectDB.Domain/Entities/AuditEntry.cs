using ProtectDB.Domain.Common;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Domain.Entities;

// Запись журнала аудита: кто, что, когда и с каким результатом выполнил.
public class AuditEntry : Entity
{
    public DateTime TimestampUtc { get; set; }        // время события

    public int? UserId { get; set; }                  // автор (может быть неизвестен)
    public User? User { get; set; }
    public string UserName { get; set; } = "system";  // снимок логина на момент события

    public AuditAction Action { get; set; }           // действие
    public AuditResult Result { get; set; }           // результат

    public string? EntityType { get; set; }           // имя сущности (User, Role, ...)
    public string? EntityId { get; set; }             // идентификатор сущности
    public string? Details { get; set; }              // подробности
    public string? IpAddress { get; set; }            // сетевой адрес
}
