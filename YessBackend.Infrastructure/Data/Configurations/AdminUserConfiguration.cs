using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YessBackend.Domain.Entities;

namespace YessBackend.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация для AdminUser entity
/// Соответствует таблице "AdminUsers" с UUID и enum admin_role
/// </summary>
public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        // Имя таблицы как в БД (с большой буквы)
        builder.ToTable("AdminUsers");

        // Настройка UUID для Id
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        // Username - VARCHAR(100) в таблице
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        // Email - VARCHAR(255)
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        // PasswordHash - TEXT
        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Role - маппится на enum admin_role в PostgreSQL
        // Храним как string в модели, но в БД это enum
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("admin");
        // Примечание: EF Core будет работать со строкой,
        // но в БД колонка имеет тип admin_role enum

        // IsActive - BOOLEAN
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps - TIMESTAMPTZ
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Индексы (уникальные уже есть в таблице)
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Role).HasDatabaseName("idx_admin_user_role");
    }
}

