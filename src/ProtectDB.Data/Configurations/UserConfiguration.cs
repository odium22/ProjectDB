using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы Users.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(128);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(256);

        // Логин уникален.
        builder.HasIndex(u => u.UserName).IsUnique();
    }
}
