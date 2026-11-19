using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Abstractions;
using ProtectDB.Data;

namespace ProtectDB.Application.Services;

// Проверка прав через объединение прав всех ролей пользователя.
public class AccessControlService : IAccessControlService
{
    private readonly ProtectDbContext _db;

    public AccessControlService(ProtectDbContext db) => _db = db;

    public Task<bool> HasPermissionAsync(int userId, string permissionCode, CancellationToken ct = default)
        => _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .AnyAsync(rp => rp.Permission!.Code == permissionCode, ct);

    public Task<bool> HasRoleAsync(int userId, string roleName, CancellationToken ct = default)
        => _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.Role!.Name == roleName, ct);

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(int userId, CancellationToken ct = default)
        => await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(ct);
}
