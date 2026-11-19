using System.Security.Cryptography;

namespace ProtectDB.Application.Security;

// Хэширование пароля методом PBKDF2 (SHA-256), случайная соль, 100 000 итераций.
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // размер соли, байт
    private const int HashSize = 32;      // размер хэша, байт
    private const int Iterations = 100_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        var saltBytes = Convert.FromBase64String(salt);
        var expected = Convert.FromBase64String(hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, expected.Length);

        // Сравнение за постоянное время для защиты от атак по времени отклика.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
