namespace ProtectDB.Application.Dtos;

// Представление защищаемого ресурса для UI/API.
public class ResourceDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsProtected { get; init; }
}
