namespace ProtectDB.Domain.Entities;

// Связь роль-право (составной первичный ключ).
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public DateTime GrantedAtUtc { get; set; } // когда выдано право
}
