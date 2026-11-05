using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы Sessions.
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Token).IsRequired().HasMaxLength(128);
        builder.Property(s => s.IpAddress).HasMaxLength(64);

        builder.HasIndex(s => s.Token).IsUnique();

        // Вычисляемое свойство в БД не хранится.
        builder.Ignore(s => s.IsActive);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
