namespace ProtectDB.Domain.Enums;

// Результат зафиксированного действия.
public enum AuditResult
{
    Success, // операция выполнена
    Failure  // операция отклонена или завершилась ошибкой
}
