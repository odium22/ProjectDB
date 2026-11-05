using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;

namespace ProtectDB.Application.Abstractions;

// Управление учётными записями.
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<OperationResult<int>> CreateAsync(
        string userName, string displayName, string password, string? email = null, CancellationToken ct = default);

    Task<OperationResult> UpdateAsync(
        int id, string displayName, string? email, CancellationToken ct = default);

    Task<OperationResult> ChangePasswordAsync(int id, string newPassword, CancellationToken ct = default);

    Task<OperationResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    Task<OperationResult> LockAsync(int id, CancellationToken ct = default);
    Task<OperationResult> UnlockAsync(int id, CancellationToken ct = default);

    Task<OperationResult> AssignRoleAsync(int userId, string roleName, CancellationToken ct = default);
    Task<OperationResult> RemoveRoleAsync(int userId, string roleName, CancellationToken ct = default);
}
