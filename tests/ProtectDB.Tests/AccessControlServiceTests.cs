using ProtectDB.Application.Services;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Tests;

// Проверки расчёта прав по ролям.
public class AccessControlServiceTests
{
    [Fact]
    public async Task HasPermission_ReflectsRolePermissions()
    {
        using var db = new TestDatabase();
        await using var context = db.CreateContext();

        var user = new User { UserName = "operator", DisplayName = "Operator", CreatedAtUtc = DateTime.UtcNow };
        var role = new Role { Name = "Operator" };
        var permission = new Permission { Code = "users.create", Name = "Создание пользователей" };

        context.AddRange(user, role, permission);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await context.SaveChangesAsync();

        var service = new AccessControlService(context);

        Assert.True(await service.HasPermissionAsync(user.Id, "users.create"));
        Assert.False(await service.HasPermissionAsync(user.Id, "users.delete"));
        Assert.True(await service.HasRoleAsync(user.Id, "Operator"));

        var effective = await service.GetEffectivePermissionsAsync(user.Id);
        Assert.Contains("users.create", effective);
    }
}
