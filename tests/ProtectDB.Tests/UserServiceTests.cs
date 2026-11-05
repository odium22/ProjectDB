using ProtectDB.Application.Security;
using ProtectDB.Application.Services;

namespace ProtectDB.Tests;

// Проверки управления учётными записями.
public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_RejectsDuplicateUserName()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var current = new CurrentUserContext();
        await using var context = db.CreateContext();

        var service = new UserService(context, hasher, new AuditService(context, current));

        var first = await service.CreateAsync("dave", "Dave", "Pass#1");
        var second = await service.CreateAsync("dave", "Dave Two", "Pass#2");

        Assert.True(first.Success);
        Assert.False(second.Success);
    }

    [Fact]
    public async Task AssignRole_GrantsAccess()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var current = new CurrentUserContext();
        await using var context = db.CreateContext();

        var permission = new ProtectDB.Domain.Entities.Permission { Code = "audit.read", Name = "Аудит" };
        var role = new ProtectDB.Domain.Entities.Role { Name = "Auditor" };
        context.AddRange(permission, role);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new ProtectDB.Domain.Entities.RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        });
        await context.SaveChangesAsync();

        var users = new UserService(context, hasher, new AuditService(context, current));
        var created = await users.CreateAsync("erin", "Erin", "Pass#1");

        var assign = await users.AssignRoleAsync(created.Value, "Auditor");

        Assert.True(assign.Success);

        var access = new AccessControlService(context);
        Assert.True(await access.HasPermissionAsync(created.Value, "audit.read"));
    }
}
