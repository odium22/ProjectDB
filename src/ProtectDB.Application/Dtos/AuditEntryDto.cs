using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Dtos;

// Представление записи журнала аудита для UI/API.
public class AuditEntryDto
{
    public int Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string UserName { get; init; } = string.Empty;
    public AuditAction Action { get; init; }
    public AuditResult Result { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Details { get; init; }
    public string? IpAddress { get; init; }
}
