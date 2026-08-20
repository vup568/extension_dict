using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DictionarySenseConfiguration : IEntityTypeConfiguration<DictionarySense>
{
    public void Configure(EntityTypeBuilder<DictionarySense> builder)
    {
        builder.ToTable("dictionary_senses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EntryId).HasColumnName("entry_id").IsRequired();
        builder.Property(e => e.SenseKey).HasColumnName("sense_key").IsRequired();
        builder.Property(e => e.PartOfSpeech).HasColumnName("part_of_speech");
        builder.Property(e => e.Position).HasColumnName("position").HasDefaultValue((short)0);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.EntryId).HasDatabaseName("idx_dictionary_senses_entry_id");
        builder.HasIndex(e => new { e.EntryId, e.SenseKey }).IsUnique().HasDatabaseName("uq_dictionary_senses_entry_key");

        // 1:N relationships
        builder.HasMany(e => e.Glosses).WithOne(g => g.Sense).HasForeignKey(g => g.SenseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Applicabilities).WithOne(a => a.Sense).HasForeignKey(a => a.SenseId).OnDelete(DeleteBehavior.Cascade);
    }
}
