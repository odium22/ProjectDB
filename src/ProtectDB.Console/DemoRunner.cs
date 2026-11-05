using Microsoft.Extensions.DependencyInjection;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Seeding;

namespace ProtectDB.Cli;

// Сценарный прогон без ручного ввода. Демонстрирует сквозную работу логики.
public static class DemoRunner
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var auth = services.GetRequiredService<IAuthenticationService>();
        var users = services.GetRequiredService<IUserService>();
        var access = services.GetRequiredService<IAccessControlService>();
        var audit = services.GetRequiredService<IAuditService>();

        Section("1. Вход администратора");
        var login = await auth.LoginAsync(
            DatabaseSeeder.DefaultAdminUserName, DatabaseSeeder.DefaultAdminPassword, "127.0.0.1");

        if (!login.Success)
        {
            Console.WriteLine($"Ошибка входа: {login.Error}");
            return;
        }

        Console.WriteLine($"Вход выполнен: {login.UserName}");
        Console.WriteLine($"Токен сессии: {login.Token}");

        Section("2. Текущие пользователи");
        foreach (var user in await users.GetAllAsync())
        {
            Console.WriteLine($"[{user.Id}] {user.UserName} ({user.DisplayName}) роли: {string.Join(", ", user.Roles)}");
        }

        Section("3. Создание пользователя demo.user");
        var created = await users.CreateAsync("demo.user", "Демо Пользователь", "Demo#12345", "demo@example.com");
        if (created.Success)
        {
            await users.AssignRoleAsync(created.Value, "User");
            Console.WriteLine($"Создан пользователь Id={created.Value}, выдана роль User.");
        }
        else
        {
            Console.WriteLine($"Пользователь уже существует: {created.Error}");
        }

        var allUsers = await users.GetAllAsync();
        var demoUserId = allUsers.First(u => u.UserName == "demo.user").Id;

        Section("4. Проверка прав");
        Console.WriteLine($"admin     → users.create    : {await access.HasPermissionAsync(login.UserId!.Value, "users.create")}");
        Console.WriteLine($"demo.user → users.create    : {await access.HasPermissionAsync(demoUserId, "users.create")}");
        Console.WriteLine($"demo.user → resources.read  : {await access.HasPermissionAsync(demoUserId, "resources.read")}");

        Section("5. Эффективные права demo.user");
        foreach (var permission in await access.GetEffectivePermissionsAsync(demoUserId))
        {
            Console.WriteLine($" - {permission}");
        }

        Section("6. Последние события аудита");
        foreach (var entry in await audit.QueryAsync(take: 10))
        {
            Console.WriteLine($"{entry.TimestampUtc:u} {entry.UserName,-10} {entry.Action,-16} {entry.Result,-8} {entry.Details}");
        }

        Section("7. Выход");
        await auth.LogoutAsync(login.Token ?? string.Empty);
        Console.WriteLine("Сессия завершена.");
    }

    private static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
