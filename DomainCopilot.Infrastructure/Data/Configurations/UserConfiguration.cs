using DomainCopilot.Application.Auth.DTOs;
using DomainCopilot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        // Seed 1 Admin/Supervisor and multiple Technicians
        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "supervisor",
                PasswordHash = "123456",
                FullName = "Eng. Mohamed - Lead Maintenance Supervisor",
                Role = UserRoles.Supervisor,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "tech_ahmed",
                PasswordHash = "123456",
                FullName = "Ahmed - Vibration & Bearing Specialist",
                Role = UserRoles.Technician,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "tech_omar",
                PasswordHash = "123456",
                FullName = "Omar - Electrical & LOTO Safety Tech",
                Role = UserRoles.Technician,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Username = "tech_sara",
                PasswordHash = "123456",
                FullName = "Sara - Mechanical Alignment Specialist",
                Role = UserRoles.Technician,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Username = "tech_youssef",
                PasswordHash = "123456",
                FullName = "Youssef - Lubrication & Seals Tech",
                Role = UserRoles.Technician,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
