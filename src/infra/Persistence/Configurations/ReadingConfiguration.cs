using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EntryId).HasColumnName("entry_id").IsRequired();
        builder.Property(e => e.ReadingText).HasColumnName("reading").IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority");
        builder.Property(e => e.IsCommon).HasColumnName("is_common").HasDefaultValue(false);
        builder.Property(e => e.Info).HasColumnName("info");
        builder.Property(e => e.RestrictedToForms).HasColumnName("restricted_to_forms");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.ReadingText).HasDatabaseName("idx_readings_reading");
        builder.HasIndex(e => e.EntryId).HasDatabaseName("idx_readings_entry_id");
        builder.HasIndex(e => new { e.EntryId, e.ReadingText }).IsUnique().HasDatabaseName("uq_readings_entry_reading");
    }
}
