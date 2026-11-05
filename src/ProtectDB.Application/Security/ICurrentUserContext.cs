namespace ProtectDB.Application.Security;

// Текущий пользователь, выполняющий операции (контекст сессии/запроса).
public interface ICurrentUserContext
{
    int? UserId { get; }
    string UserName { get; }

    void Set(int userId, string userName);
    void Clear();
}
