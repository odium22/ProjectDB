using ProtectDB.Domain.Common;

namespace ProtectDB.Domain.Entities;

// Право доступа к операции над ресурсом.
public class Permission : Entity
{
    public string Code { get; set; } = string.Empty; // например users.create, уникальный
    public string Name { get; set; } = string.Empty; // отображаемое имя
    public string? Description { get; set; }

    public int? ResourceId { get; set; }             // привязка к ресурсу (необязательна)
    public Resource? Resource { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
