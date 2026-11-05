using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;

namespace ProtectDB.Application.Abstractions;

// Управление защищаемыми ресурсами.
public interface IResourceService
{
    Task<IReadOnlyList<ResourceDto>> GetAllAsync(CancellationToken ct = default);

    Task<OperationResult<int>> CreateAsync(
        string code, string name, string? description = null, bool isProtected = true, CancellationToken ct = default);

    Task<OperationResult> UpdateAsync(
        int id, string name, string? description, bool isProtected, CancellationToken ct = default);
}
