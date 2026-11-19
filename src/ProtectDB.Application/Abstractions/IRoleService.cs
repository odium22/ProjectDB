using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;

namespace ProtectDB.Application.Abstractions;

// Управление ролями и их правами.
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<OperationResult<int>> CreateAsync(string name, string? description = null, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(int id, string name, string? description, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default);

    Task<OperationResult> GrantPermissionAsync(int roleId, string permissionCode, CancellationToken ct = default);
    Task<OperationResult> RevokePermissionAsync(int roleId, string permissionCode, CancellationToken ct = default);
}
