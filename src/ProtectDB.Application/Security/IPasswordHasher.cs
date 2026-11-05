namespace ProtectDB.Application.Security;

// Контракт хэширования и проверки паролей.
public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool Verify(string password, string hash, string salt);
}
