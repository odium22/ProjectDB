namespace ProtectDB.Domain.Enums;

// Действия, которые фиксируются в журнале аудита.
public enum AuditAction
{
    Create,           // создание записи
    Update,           // изменение записи
    Delete,           // удаление записи
    Login,            // успешный вход
    Logout,           // выход из системы
    LoginFailed,      // неудачная попытка входа
    AccessDenied,     // отказ в доступе
    GrantRole,        // выдача роли пользователю
    RevokeRole,       // снятие роли с пользователя
    GrantPermission,  // выдача права роли
    RevokePermission, // снятие права с роли
    Lock,             // блокировка учётной записи
    Unlock            // разблокировка учётной записи
}
