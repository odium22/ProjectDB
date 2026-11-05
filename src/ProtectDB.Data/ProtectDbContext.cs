using Microsoft.EntityFrameworkCore;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data;

// Контекст EF Core: единая точка доступа к таблицам БД SQLite.
public class ProtectDbContext : DbContext
{
    public ProtectDbContext(DbContextOptions<ProtectDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Все конфигурации сущностей подключаются автоматически из этой сборки.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProtectDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
