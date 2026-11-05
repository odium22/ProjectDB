using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;
using ProtectDB.Data;
using ProtectDB.Domain.Entities;
using ProtectDB.Domain.Enums;

namespace ProtectDB.Application.Services;

// Управление ролями и их правами.
public class RoleService : IRoleService
{
    private readonly ProtectDbContext _db;
    private readonly IAuditService _audit;

    public RoleService(ProtectDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .ToListAsync(ct);

    public Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Roles.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<int>> CreateAsync(string name, string? description = null, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult<int>.Fail("Имя роли обязательно.");
        }

        if (await _db.Roles.AnyAsync(r => r.Name == name, ct))
        {
            return OperationResult<int>.Fail("Роль с таким именем уже существует.");
        }

        var role = new Role { Name = name, Description = description };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Create, AuditResult.Success,
            nameof(Role), role.Id.ToString(), $"Создана роль {role.Name}", ct: ct);

        return OperationResult<int>.Ok(role.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        role.Name = name.Trim();
        role.Description = description;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Update, AuditResult.Success,
            nameof(Role), role.Id.ToString(), $"Изменена роль {role.Name}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        // Системные роли защищены от удаления.
        if (role.IsSystem)
        {
            await _audit.WriteAsync(AuditAction.Delete, AuditResult.Failure,
                nameof(Role), role.Id.ToString(), $"Отказ в удалении системной роли {role.Name}", ct: ct);
            return OperationResult.Fail("Системную роль удалить нельзя.");
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.Delete, AuditResult.Success,
            nameof(Role), id.ToString(), $"Удалена роль {role.Name}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> GrantPermissionAsync(int roleId, string permissionCode, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Code == permissionCode, ct);
        if (permission is null)
        {
            return OperationResult.Fail("Право не найдено.");
        }

        if (await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id, ct))
        {
            return OperationResult.Ok();
        }

        _db.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permission.Id,
            GrantedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.GrantPermission, AuditResult.Success,
            nameof(Role), role.Id.ToString(), $"Роли {role.Name} выдано право {permission.Code}", ct: ct);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> RevokePermissionAsync(int roleId, string permissionCode, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            return OperationResult.Fail("Роль не найдена.");
        }

        var link = await _db.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.Permission!.Code == permissionCode, ct);
        if (link is null)
        {
            return OperationResult.Fail("У роли нет этого права.");
        }

        _db.RolePermissions.Remove(link);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(AuditAction.RevokePermission, AuditResult.Success,
            nameof(Role), role.Id.ToString(), $"У роли {role.Name} отозвано право {permissionCode}", ct: ct);

        return OperationResult.Ok();
    }
}
