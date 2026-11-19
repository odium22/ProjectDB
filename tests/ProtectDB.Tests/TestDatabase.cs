using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProtectDB.Data;

namespace ProtectDB.Tests;

// Тестовая БД SQLite в памяти. Соединение держится открытым, пока жив объект.
internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    // ensureCreated = false оставляет схему для применения миграций (тест seeder).
    public TestDatabase(bool ensureCreated = true)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        if (ensureCreated)
        {
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }
    }

    public ProtectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProtectDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ProtectDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
