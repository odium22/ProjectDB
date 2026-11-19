using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Security;
using ProtectDB.Data;
using ProtectDB.Domain.Constants;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Application.Seeding;

// Начальное наполнение БД: ресурсы, права, системные роли и администратор.
public class DatabaseSeeder
{
    public const string DefaultAdminUserName = "admin";
    public const string DefaultAdminPassword = "Admin#12345";

    private readonly ProtectDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DatabaseSeeder(ProtectDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // Применить миграции и заполнить справочники. Повторный вызов безопасен.
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);
        await SeedResourcesAsync(ct);
        await SeedPermissionsAsync(ct);
        await SeedRolesAsync(ct);
        await SeedAdminAsync(ct);
    }

    private async Task SeedResourcesAsync(CancellationToken ct)
    {
        var resources = new[]
        {
            new Resource { Code = "users", Name = "Пользователи" },
            new Resource { Code = "roles", Name = "Роли" },
            new Resource { Code = "resources", Name = "Ресурсы" },
            new Resource { Code = "audit", Name = "Журнал аудита" }
        };

        foreach (var resource in resources)
        {
            if (!await _db.Resources.AnyAsync(r => r.Code == resource.Code, ct))
            {
                _db.Resources.Add(resource);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var resourceIds = await _db.Resources.ToDictionaryAsync(r => r.Code, r => r.Id, ct);

        var permissions = new (string Code, string Name, string Resource)[]
        {
            (PermissionCodes.UsersRead, "Просмотр пользователей", "users"),
            (PermissionCodes.UsersCreate, "Создание пользователей", "users"),
            (PermissionCodes.UsersUpdate, "Изменение пользователей", "users"),
            (PermissionCodes.UsersDelete, "Удаление пользователей", "users"),
            (PermissionCodes.RolesRead, "Просмотр ролей", "roles"),
            (PermissionCodes.RolesManage, "Управление ролями", "roles"),
            (PermissionCodes.ResourcesRead, "Просмотр ресурсов", "resources"),
            (PermissionCodes.ResourcesManage, "Управление ресурсами", "resources"),
            (PermissionCodes.AuditRead, "Просмотр журнала аудита", "audit"),
            (PermissionCodes.SessionsManage, "Управление сессиями", "users")
        };

        foreach (var (code, name, resource) in permissions)
        {
            if (!await _db.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                _db.Permissions.Add(new Permission
                {
                    Code = code,
                    Name = name,
                    ResourceId = resourceIds.TryGetValue(resource, out var id) ? id : null
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var admin = await EnsureRoleAsync(RoleNames.Administrator, "Полный доступ к системе", ct);
        var auditor = await EnsureRoleAsync(RoleNames.Auditor, "Просмотр журнала аудита", ct);
        var user = await EnsureRoleAsync(RoleNames.User, "Базовый доступ", ct);

        // Администратор получает все права.
        var allPermissionIds = await _db.Permissions.Select(p => p.Id).ToListAsync(ct);
        await EnsureRolePermissionsAsync(admin.Id, allPermissionIds, ct);

        // Аудитор видит только журнал.
        await EnsureRolePermissionsAsync(auditor.Id, await PermissionIdsAsync(PermissionCodes.AuditRead, ct), ct);

        // Пользователь видит ресурсы.
        await EnsureRolePermissionsAsync(user.Id, await PermissionIdsAsync(PermissionCodes.ResourcesRead, ct), ct);
    }

    private async Task<Role> EnsureRoleAsync(string name, string description, CancellationToken ct)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
        if (role is not null)
        {
            return role;
        }

        role = new Role { Name = name, Description = description, IsSystem = true };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    private async Task EnsureRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken ct)
    {
        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        foreach (var permissionId in permissionIds.Except(existing))
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<int>> PermissionIdsAsync(string code, CancellationToken ct)
        => await _db.Permissions.Where(p => p.Code == code).Select(p => p.Id).ToListAsync(ct);

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == DefaultAdminUserName, ct))
        {
            return;
        }

        var (hash, salt) = _hasher.HashPassword(DefaultAdminPassword);
        var admin = new User
        {
            UserName = DefaultAdminUserName,
            DisplayName = "Администратор",
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync(ct);

        var adminRole = await _db.Roles.FirstAsync(r => r.Name == RoleNames.Administrator, ct);
        _db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id, AssignedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}
