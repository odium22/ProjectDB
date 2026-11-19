namespace ProtectDB.Application.Dtos;

// Представление пользователя для UI/API.
public class UserDto
{
    public int Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public bool IsLocked { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
