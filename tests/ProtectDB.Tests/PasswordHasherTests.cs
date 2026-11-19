using ProtectDB.Application.Security;

namespace ProtectDB.Tests;

// Проверки хэширования паролей.
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotStorePlainPassword()
    {
        var (hash, salt) = _hasher.HashPassword("Secret#1");

        Assert.NotEqual("Secret#1", hash);
        Assert.False(string.IsNullOrWhiteSpace(salt));
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword()
    {
        var (hash, salt) = _hasher.HashPassword("Secret#1");

        Assert.True(_hasher.Verify("Secret#1", hash, salt));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var (hash, salt) = _hasher.HashPassword("Secret#1");

        Assert.False(_hasher.Verify("secret#1", hash, salt));
    }

    [Fact]
    public void Salt_IsUniquePerHash()
    {
        var first = _hasher.HashPassword("Secret#1");
        var second = _hasher.HashPassword("Secret#1");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.NotEqual(first.Hash, second.Hash);
    }
}
