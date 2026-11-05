namespace ProtectDB.Domain.Constants;

// Коды прав, используемые в логике и при начальном наполнении БД.
public static class PermissionCodes
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";

    public const string ResourcesRead = "resources.read";
    public const string ResourcesManage = "resources.manage";

    public const string AuditRead = "audit.read";
    public const string SessionsManage = "sessions.manage";
}
