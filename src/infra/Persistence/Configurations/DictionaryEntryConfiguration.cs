using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DictionaryEntryConfiguration : IEntityTypeConfiguration<DictionaryEntry>
{
    public void Configure(EntityTypeBuilder<DictionaryEntry> builder)
    {
        builder.ToTable("dictionary_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CanonicalId).HasColumnName("canonical_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.CanonicalId).IsUnique().HasDatabaseName("uq_dictionary_entries_canonical_id");

        // 1:N relationships
        builder.HasMany(e => e.WrittenForms).WithOne(f => f.Entry).HasForeignKey(f => f.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Readings).WithOne(r => r.Entry).HasForeignKey(r => r.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Senses).WithOne(s => s.Entry).HasForeignKey(s => s.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}
