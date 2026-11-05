using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Seeding;
using ProtectDB.Application.Security;
using ProtectDB.Domain.Constants;

namespace ProtectDB.Tests;

// Проверки начального наполнения БД и идемпотентности.
public class DatabaseSeederTests
{
    [Fact]
    public async Task Seed_CreatesSchemaAndDefaultData_WithoutDuplicates()
    {
        using var db = new TestDatabase(ensureCreated: false);
        var seeder = new DatabaseSeeder(db.CreateContext(), new PasswordHasher());

        await seeder.SeedAsync();
        await seeder.SeedAsync(); // повторный запуск не должен дублировать данные

        await using var context = db.CreateContext();

        Assert.Equal(1, await context.Users.CountAsync(u => u.UserName == DatabaseSeeder.DefaultAdminUserName));
        Assert.True(await context.Roles.AnyAsync(r => r.Name == RoleNames.Administrator));
        Assert.True(await context.Permissions.AnyAsync(p => p.Code == PermissionCodes.UsersCreate));
    }
}
