using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProtectDB.Data;

// Фабрика контекста для команд dotnet ef (создание миграций без запуска приложения).
public class ProtectDbContextFactory : IDesignTimeDbContextFactory<ProtectDbContext>
{
    public ProtectDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProtectDbContext>()
            .UseSqlite("Data Source=protectdb.db")
            .Options;

        return new ProtectDbContext(options);
    }
}
