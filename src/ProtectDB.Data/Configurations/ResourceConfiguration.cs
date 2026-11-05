using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы Resources.
public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Description).HasMaxLength(256);

        builder.HasIndex(r => r.Code).IsUnique();
    }
}
