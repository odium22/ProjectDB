using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;
using ProtectDB.Data;
using ProtectDB.Domain.Entities;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Services;

// Управление защищаемыми ресурсами.
public class ResourceService : IResourceService
{
    private readonly ProtectDbContext _db;
    private readonly IAuditService _audit;

    public ResourceService(ProtectDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ResourceDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.Resources.AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new ResourceDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IsProtected = r.IsProtected
            })
            .ToListAsync(ct);

    public async Task<OperationResult<int>> CreateAsync(
        string code, string name, string? description = null, bool isProtected = true, CancellationToken ct = default)
    {
        code = code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return OperationResult<int>.Fail("Код и имя ресурса обязательны.");
        }

        if (await _db.Resources.AnyAsync(r => r.Code == code, ct))
        {
            return OperationResult<int>.Fail("Ресурс с таким кодом уже существует.");
        }

        var resource = new Resource { Code = code, Name = name.Trim(), Description = description, IsProtected = isProtected };
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Create, AuditResult.Success,
            nameof(Resource), resource.Id.ToString(), $"Создан ресурс {resource.Code}", ct: ct);

        return OperationResult<int>.Ok(resource.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, string name, string? description, bool isProtected, CancellationToken ct = default)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (resource is null)
        {
            return OperationResult.Fail("Ресурс не найден.");
        }

        resource.Name = name.Trim();
        resource.Description = description;
        resource.IsProtected = isProtected;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(Resource), resource.Id.ToString(), $"Изменён ресурс {resource.Code}", ct: ct);

        return OperationResult.Ok();
    }
}
