using DomainCopilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Data.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentId)
            .IsRequired();

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        builder.Property(c => c.Text)
            .IsRequired();

        builder.Property(c => c.Source)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Section)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PageNumber);

        builder.Property(c => c.Clause)
            .HasMaxLength(200);

        builder.Property(c => c.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Embedding)
            .HasColumnType("real[]");

        builder.HasIndex(c => c.DocumentId);

        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
            .IsUnique();
    }
}
