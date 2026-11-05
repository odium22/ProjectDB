using ProtectDB.Domain.Common;

namespace ProtectDB.Domain.Entities;

// Защищаемый ресурс системы, к которому привязаны права.
public class Resource : Entity
{
    public string Code { get; set; } = string.Empty;   // машинный код, уникальный
    public string Name { get; set; } = string.Empty;   // отображаемое имя
    public string? Description { get; set; }
    public bool IsProtected { get; set; } = true;      // требует проверки прав

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
