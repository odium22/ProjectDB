using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы Roles.
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Description).HasMaxLength(256);

        // Имя роли уникально.
        builder.HasIndex(r => r.Name).IsUnique();
    }
}
