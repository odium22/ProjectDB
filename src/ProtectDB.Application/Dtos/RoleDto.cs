namespace ProtectDB.Application.Dtos;

// Представление роли для UI/API.
public class RoleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
