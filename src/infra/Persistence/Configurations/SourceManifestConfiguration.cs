using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SourceManifestConfiguration : IEntityTypeConfiguration<SourceManifest>
{
    public void Configure(EntityTypeBuilder<SourceManifest> builder)
    {
        builder.ToTable("source_manifests");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SourceName).HasColumnName("source_name").IsRequired();
        builder.Property(e => e.Publisher).HasColumnName("publisher");
        builder.Property(e => e.Version).HasColumnName("version").IsRequired();
        builder.Property(e => e.License).HasColumnName("license");
        builder.Property(e => e.Attribution).HasColumnName("attribution");
        builder.Property(e => e.Checksum).HasColumnName("checksum");
        builder.Property(e => e.RecordCount).HasColumnName("record_count");
        builder.Property(e => e.RejectedCount).HasColumnName("rejected_count");
        builder.Property(e => e.TransformationId).HasColumnName("transformation_id");
        builder.Property(e => e.ValidationResult).HasColumnName("validation_result");
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.SourceName, e.Version }).IsUnique().HasDatabaseName("uq_source_manifests_name_version");

        builder.HasMany(e => e.Records).WithOne(r => r.Manifest).HasForeignKey(r => r.ManifestId).OnDelete(DeleteBehavior.Cascade);
    }
}
