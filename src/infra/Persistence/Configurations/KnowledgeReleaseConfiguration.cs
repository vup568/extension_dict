using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class KnowledgeReleaseConfiguration : IEntityTypeConfiguration<KnowledgeRelease>
{
    public void Configure(EntityTypeBuilder<KnowledgeRelease> builder)
    {
        builder.ToTable("knowledge_releases");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Version).HasColumnName("version").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.PublishedAt).HasColumnName("published_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.Version).IsUnique().HasDatabaseName("uq_knowledge_releases_version");

        builder.HasMany(e => e.Revisions).WithOne(r => r.Release).HasForeignKey(r => r.ReleaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
