using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class EditorialMappingConfiguration : IEntityTypeConfiguration<EditorialMapping>
{
    public void Configure(EntityTypeBuilder<EditorialMapping> builder)
    {
        builder.ToTable("editorial_mappings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SourceRecordId).HasColumnName("source_record_id").IsRequired();
        builder.Property(e => e.TargetResourceType).HasColumnName("target_resource_type").IsRequired();
        builder.Property(e => e.TargetResourceId).HasColumnName("target_resource_id").IsRequired();
        builder.Property(e => e.MappingType).HasColumnName("mapping_type").IsRequired();
        builder.Property(e => e.DecisionReason).HasColumnName("decision_reason");
        builder.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.SourceRecordId, e.TargetResourceType, e.TargetResourceId })
            .IsUnique().HasDatabaseName("uq_editorial_mappings_source_target");
        builder.HasIndex(e => new { e.TargetResourceType, e.TargetResourceId })
            .HasDatabaseName("idx_editorial_mappings_target");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_editorial_mappings_resource_type",
                "target_resource_type IN ('dictionary', 'kanji', 'grammar')");
            t.HasCheckConstraint("chk_editorial_mappings_type",
                "mapping_type IN ('link', 'merge', 'split', 'retire')");
        });
    }
}
