namespace ProtectDB.Application.Abstractions;

// Проверка прав пользователя на основе назначенных ролей.
public interface IAccessControlService
{
    Task<bool> HasPermissionAsync(int userId, string permissionCode, CancellationToken ct = default);
    Task<bool> HasRoleAsync(int userId, string roleName, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(int userId, CancellationToken ct = default);
}
