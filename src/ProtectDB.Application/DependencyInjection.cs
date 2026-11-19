using Microsoft.Extensions.DependencyInjection;
using ProtectDB.Application.Abstractions;
using ProtectDB.Application.Security;
using ProtectDB.Application.Seeding;
using ProtectDB.Application.Services;

namespace ProtectDB.Application;

// Регистрация сервисов прикладного слоя в DI-контейнере.
public static class DependencyInjection
{
    public static IServiceCollection AddProtectDbApplication(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
