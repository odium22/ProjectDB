using ProtectDB.Application.Common;
using ProtectDB.Application.Dtos;

namespace ProtectDB.Application.Abstractions;

// Аутентификация: вход, выход и управление сессиями.
public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(
        string userName, string password, string? ipAddress = null, CancellationToken ct = default);

    Task LogoutAsync(string token, CancellationToken ct = default);
    Task<OperationResult> RevokeSessionAsync(int sessionId, CancellationToken ct = default);
}
