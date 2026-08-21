using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SourceRecordConfiguration : IEntityTypeConfiguration<SourceRecord>
{
    public void Configure(EntityTypeBuilder<SourceRecord> builder)
    {
        builder.ToTable("source_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ManifestId).HasColumnName("manifest_id").IsRequired();
        builder.Property(e => e.SourceIdentity).HasColumnName("source_identity").IsRequired();
        builder.Property(e => e.RecordData).HasColumnName("record_data").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.ManifestId, e.SourceIdentity }).IsUnique().HasDatabaseName("uq_source_records_manifest_identity");
        builder.HasIndex(e => e.SourceIdentity).HasDatabaseName("idx_source_records_identity");

        builder.HasMany(e => e.EditorialMappings).WithOne(m => m.SourceRecord).HasForeignKey(m => m.SourceRecordId).OnDelete(DeleteBehavior.Cascade);
    }
}
