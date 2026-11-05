using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProtectDB.Domain.Entities;

namespace ProtectDB.Data.Configurations;

// Настройка таблицы AuditEntries.
public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).IsRequired().HasMaxLength(64);
        builder.Property(a => a.EntityType).HasMaxLength(64);
        builder.Property(a => a.EntityId).HasMaxLength(64);
        builder.Property(a => a.Details).HasMaxLength(1024);
        builder.Property(a => a.IpAddress).HasMaxLength(64);

        // Действие и результат хранятся строками для читаемости отчётов.
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.Result).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Частый запрос: последние события.
        builder.HasIndex(a => a.TimestampUtc);

        // При удалении пользователя история сохраняется (автор обнуляется).
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
