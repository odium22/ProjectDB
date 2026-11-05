using ProtectDB.Application.Dtos;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Abstractions;

// Запись и выборка событий журнала аудита.
public interface IAuditService
{
    // Записать событие. Автор берётся из текущего контекста, если не задан явно.
    Task WriteAsync(
        AuditAction action,
        AuditResult result,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        int? userId = null,
        string? userName = null,
        CancellationToken ct = default);

    // Выбрать события по фильтрам (новые сверху).
    Task<IReadOnlyList<AuditEntryDto>> QueryAsync(
        int? userId = null,
        AuditAction? action = null,
        AuditResult? result = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int take = 200,
        CancellationToken ct = default);
}
