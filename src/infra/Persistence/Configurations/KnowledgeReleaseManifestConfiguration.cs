using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class KnowledgeReleaseManifestConfiguration : IEntityTypeConfiguration<KnowledgeReleaseManifest>
{
    public void Configure(EntityTypeBuilder<KnowledgeReleaseManifest> builder)
    {
        builder.ToTable("knowledge_release_manifests");

        // Composite PK
        builder.HasKey(e => new { e.ReleaseId, e.ManifestId });
        builder.Property(e => e.ReleaseId).HasColumnName("release_id");
        builder.Property(e => e.ManifestId).HasColumnName("manifest_id");

        builder.HasOne(e => e.Release)
            .WithMany(r => r.ReleaseManifests)
            .HasForeignKey(e => e.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Manifest)
            .WithMany(m => m.ReleaseManifests)
            .HasForeignKey(e => e.ManifestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
