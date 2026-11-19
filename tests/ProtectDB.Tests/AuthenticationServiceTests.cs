using Microsoft.EntityFrameworkCore;
using ProtectDB.Application.Security;
using ProtectDB.Application.Services;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Tests;

// Проверки входа, блокировки и сессий.
public class AuthenticationServiceTests
{
    private static User NewUser(string userName, string password, PasswordHasher hasher)
    {
        var (hash, salt) = hasher.HashPassword(password);
        return new User
        {
            UserName = userName,
            DisplayName = userName,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Login_Succeeds_WithCorrectPassword()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var current = new CurrentUserContext();
        await using var context = db.CreateContext();

        context.Users.Add(NewUser("alice", "Pass#1", hasher));
        await context.SaveChangesAsync();

        var service = new AuthenticationService(context, hasher, new AuditService(context, current), current);

        var result = await service.LoginAsync("alice", "Pass#1");

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("alice", current.UserName);
    }

    [Fact]
    public async Task Login_LocksAccount_AfterMaxFailures()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var current = new CurrentUserContext();
        await using var context = db.CreateContext();

        var user = NewUser("bob", "Pass#1", hasher);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthenticationService(context, hasher, new AuditService(context, current), current);

        for (var i = 0; i < AuthenticationService.MaxFailedLoginAttempts; i++)
        {
            var attempt = await service.LoginAsync("bob", "wrong-password");
            Assert.False(attempt.Success);
        }

        var stored = await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        Assert.True(stored.IsLocked);
    }

    [Fact]
    public async Task Login_RejectsInactiveUser()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var current = new CurrentUserContext();
        await using var context = db.CreateContext();

        var user = NewUser("carol", "Pass#1", hasher);
        user.IsActive = false;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthenticationService(context, hasher, new AuditService(context, current), current);

        var result = await service.LoginAsync("carol", "Pass#1");

        Assert.False(result.Success);
    }
}
