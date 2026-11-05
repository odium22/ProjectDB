using ProtectDB.Domain.Common;

namespace ProtectDB.Domain.Entities;

// Роль: именованный набор прав.
public class Role : Entity
{
    public string Name { get; set; } = string.Empty; // уникальное имя роли
    public string? Description { get; set; }         // описание
    public bool IsSystem { get; set; }               // системную роль нельзя удалить

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
