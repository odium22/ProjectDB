using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtectDB.Application;
using ProtectDB.Application.Seeding;
using ProtectDB.Cli;
using ProtectDB.Data;

// Точка входа временного консольного драйвера.
// Позже этот слой выносится в отдельное UI (WPF) и/или Web API для фронтенда.

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("ProtectDb") ?? "Data Source=protectdb.db";

var services = new ServiceCollection();
services.AddProtectDbData(connectionString);
services.AddProtectDbApplication();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var serviceProvider = scope.ServiceProvider;

// Применяем миграции и начальное наполнение БД.
var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
await seeder.SeedAsync();

// --demo — сценарный прогон без ввода; иначе интерактивное меню.
if (args.Contains("--demo"))
{
    await DemoRunner.RunAsync(serviceProvider);
}
else
{
    await AdminConsole.RunAsync(serviceProvider);
}
