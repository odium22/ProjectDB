using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Dtos;
using ProtectDB.Application.Security;
using ProtectDB.Data;
using ProtectDB.Domain.Entities;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Services;

// Запись и выборка событий аудита.
public class AuditService : IAuditService
{
    private readonly ProtectDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public AuditService(ProtectDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        AuditAction action,
        AuditResult result,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        int? userId = null,
        string? userName = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = userId ?? _currentUser.UserId,
            UserName = userName ?? _currentUser.UserName,
            Action = action,
            Result = result,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        };

        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEntryDto>> QueryAsync(
        int? userId = null,
        AuditAction? action = null,
        AuditResult? result = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int take = 200,
        CancellationToken ct = default)
    {
        var query = _db.AuditEntries.AsNoTracking().AsQueryable();

        if (userId is not null)
        {
            query = query.Where(e => e.UserId == userId);
        }

        if (action is not null)
        {
            query = query.Where(e => e.Action == action);
        }

        if (result is not null)
        {
            query = query.Where(e => e.Result == result);
        }

        if (fromUtc is not null)
        {
            query = query.Where(e => e.TimestampUtc >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(e => e.TimestampUtc <= toUtc);
        }

        return await query
            .OrderByDescending(e => e.TimestampUtc)
            .Take(take)
            .Select(e => new AuditEntryDto
            {
                Id = e.Id,
                TimestampUtc = e.TimestampUtc,
                UserName = e.UserName,
                Action = e.Action,
                Result = e.Result,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Details = e.Details,
                IpAddress = e.IpAddress
            })
            .ToListAsync(ct);
    }
}
