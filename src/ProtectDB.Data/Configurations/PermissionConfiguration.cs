using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы Permissions.
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(128);
        builder.Property(p => p.Description).HasMaxLength(256);

        builder.HasIndex(p => p.Code).IsUnique();

        // Право ссылается на ресурс; при удалении ресурса связь обнуляется.
        builder.HasOne(p => p.Resource)
            .WithMany(r => r.Permissions)
            .HasForeignKey(p => p.ResourceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
