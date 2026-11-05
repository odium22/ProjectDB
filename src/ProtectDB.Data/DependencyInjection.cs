using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProtectDB.Data;

// Регистрация слоя данных в DI-контейнере.
public static class DependencyInjection
{
    public static IServiceCollection AddProtectDbData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProtectDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }
}
