namespace ProtectDB.Application.Dtos;

// Результат попытки входа.
public class LoginResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Token { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
