using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Dtos;
using ProtectDB.Application.Seeding;

namespace ProtectDB.Cli;

// Консольный интерфейс администратора. Временная замена будущего UI.
public static class AdminConsole
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var auth = services.GetRequiredService<IAuthenticationService>();

        PrintHeader();
        var session = await LoginAsync(auth);
        if (session is null)
        {
            Console.WriteLine("Вход не выполнен. Завершение работы.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Добро пожаловать, {session.UserName}.");
        Console.WriteLine($"Токен сессии: {session.Token}");
        Console.WriteLine($"Действует до (UTC): {session.ExpiresAtUtc:u}");

        var exit = false;
        while (!exit)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await ListUsersAsync(services);
                    break;
                case "2":
                    await CreateUserAsync(services);
                    break;
                case "3":
                    await ListRolesAsync(services);
                    break;
                case "4":
                    await ListAuditAsync(services);
                    break;
                case "5":
                    await CheckPermissionAsync(services);
                    break;
                case "0":
                    await auth.LogoutAsync(session.Token ?? string.Empty);
                    Console.WriteLine("Сессия завершена.");
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Неизвестная команда.");
                    break;
            }
        }
    }

    private static void PrintHeader()
    {
        Console.WriteLine("========================================");
        Console.WriteLine(" ProtectDB — администрирование доступа");
        Console.WriteLine("========================================");
    }

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(" 1 - список пользователей");
        Console.WriteLine(" 2 - создать пользователя");
        Console.WriteLine(" 3 - список ролей");
        Console.WriteLine(" 4 - последние события аудита");
        Console.WriteLine(" 5 - проверить право пользователя");
        Console.WriteLine(" 0 - выход");
        Console.WriteLine("----------------------------------------");
        Console.Write("Выбор: ");
    }

    private static async Task<LoginResult?> LoginAsync(IAuthenticationService auth)
    {
        Console.Write($"Логин (Enter = {DatabaseSeeder.DefaultAdminUserName}): ");
        var userName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = DatabaseSeeder.DefaultAdminUserName;
        }

        Console.Write("Пароль: ");
        var password = ReadPassword();

        var result = await auth.LoginAsync(userName, password);
        if (!result.Success)
        {
            Console.WriteLine($"Ошибка входа: {result.Error}");
            return null;
        }

        return result;
    }

    // Ввод пароля без отображения символов.
    private static string ReadPassword()
    {
        var buffer = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                }
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
            }
        }

        return buffer.ToString();
    }

    private static async Task ListUsersAsync(IServiceProvider services)
    {
        var users = await services.GetRequiredService<IUserService>().GetAllAsync();

        Console.WriteLine();
        Console.WriteLine($"{"Id",-4} {"Логин",-16} {"Имя",-22} {"Активен",-8} {"Блок",-6} Роли");
        foreach (var user in users)
        {
            var roles = string.Join(", ", user.Roles);
            Console.WriteLine($"{user.Id,-4} {user.UserName,-16} {user.DisplayName,-22} {user.IsActive,-8} {user.IsLocked,-6} {roles}");
        }
    }

    private static async Task CreateUserAsync(IServiceProvider services)
    {
        Console.Write("Логин: ");
        var userName = Console.ReadLine() ?? string.Empty;

        Console.Write("Отображаемое имя: ");
        var displayName = Console.ReadLine() ?? string.Empty;

        Console.Write("Пароль: ");
        var password = ReadPassword();

        Console.Write("Email (Enter = пусто): ");
        var email = Console.ReadLine();

        var result = await services.GetRequiredService<IUserService>()
            .CreateAsync(userName, displayName, password, string.IsNullOrWhiteSpace(email) ? null : email);

        if (!result.Success)
        {
            Console.WriteLine($"Ошибка: {result.Error}");
            return;
        }

        // По умолчанию новому пользователю выдаётся базовая роль.
        await services.GetRequiredService<IUserService>().AssignRoleAsync(result.Value, "User");
        Console.WriteLine($"Пользователь создан (Id={result.Value}) с ролью User.");
    }

    private static async Task ListRolesAsync(IServiceProvider services)
    {
        var roles = await services.GetRequiredService<IRoleService>().GetAllAsync();

        Console.WriteLine();
        foreach (var role in roles)
        {
            var kind = role.IsSystem ? "системная" : "пользовательская";
            Console.WriteLine($"[{role.Id}] {role.Name} ({kind}) — {role.Description}");
            Console.WriteLine($"     права: {string.Join(", ", role.Permissions)}");
        }
    }

    private static async Task ListAuditAsync(IServiceProvider services)
    {
        var entries = await services.GetRequiredService<IAuditService>().QueryAsync(take: 20);

        Console.WriteLine();
        Console.WriteLine($"{"Время (UTC)",-20} {"Пользователь",-14} {"Действие",-16} {"Результат",-9} Детали");
        foreach (var entry in entries)
        {
            Console.WriteLine($"{entry.TimestampUtc,-20:u} {entry.UserName,-14} {entry.Action,-16} {entry.Result,-9} {entry.Details}");
        }
    }

    private static async Task CheckPermissionAsync(IServiceProvider services)
    {
        Console.Write("Id пользователя: ");
        if (!int.TryParse(Console.ReadLine(), out var userId))
        {
            Console.WriteLine("Некорректный Id.");
            return;
        }

        Console.Write("Код права (например users.create): ");
        var code = Console.ReadLine() ?? string.Empty;

        var allowed = await services.GetRequiredService<IAccessControlService>().HasPermissionAsync(userId, code);
        Console.WriteLine(allowed ? "Доступ разрешён." : "Доступ запрещён.");
    }
}
