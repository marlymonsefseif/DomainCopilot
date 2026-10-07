using DomainCopilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Source)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Format)
            .IsRequired();

        builder.Property(d => d.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .IsRequired();

        builder.Property(d => d.ContentHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(d => d.ContentHash)
            .IsUnique();

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.ProcessedAtUtc);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(2000);
    }
}
