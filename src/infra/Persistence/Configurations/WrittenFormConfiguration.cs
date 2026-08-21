using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WrittenFormConfiguration : IEntityTypeConfiguration<WrittenForm>
{
    public void Configure(EntityTypeBuilder<WrittenForm> builder)
    {
        builder.ToTable("written_forms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EntryId).HasColumnName("entry_id").IsRequired();
        builder.Property(e => e.Form).HasColumnName("form").IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority");
        builder.Property(e => e.IsCommon).HasColumnName("is_common").HasDefaultValue(false);
        builder.Property(e => e.Info).HasColumnName("info");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.Form).HasDatabaseName("idx_written_forms_form");
        builder.HasIndex(e => e.EntryId).HasDatabaseName("idx_written_forms_entry_id");
        builder.HasIndex(e => new { e.EntryId, e.Form }).IsUnique().HasDatabaseName("uq_written_forms_entry_form");
    }
}
