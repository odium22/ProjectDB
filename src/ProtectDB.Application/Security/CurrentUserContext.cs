namespace ProtectDB.Application.Security;

// Хранит автора операций в рамках одной сессии приложения.
public class CurrentUserContext : ICurrentUserContext
{
    public int? UserId { get; private set; }
    public string UserName { get; private set; } = "system";

    public void Set(int userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }

    public void Clear()
    {
        UserId = null;
        UserName = "system";
    }
}
