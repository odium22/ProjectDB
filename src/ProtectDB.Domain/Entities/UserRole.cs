namespace ProtectDB.Domain.Entities;

// Связь пользователь-роль (составной первичный ключ).
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } // когда выдана роль
}
