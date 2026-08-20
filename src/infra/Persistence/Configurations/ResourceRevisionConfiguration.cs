using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ResourceRevisionConfiguration : IEntityTypeConfiguration<ResourceRevision>
{
    public void Configure(EntityTypeBuilder<ResourceRevision> builder)
    {
        builder.ToTable("resource_revisions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").IsRequired();
        builder.Property(e => e.ResourceType).HasColumnName("resource_type").IsRequired();
        builder.Property(e => e.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(e => e.RevisionData).HasColumnName("revision_data").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.ReleaseId, e.ResourceType, e.ResourceId })
            .IsUnique().HasDatabaseName("uq_resource_revisions_release_resource");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_resource_revisions_type",
            "resource_type IN ('dictionary', 'kanji', 'grammar')"));
    }
}
