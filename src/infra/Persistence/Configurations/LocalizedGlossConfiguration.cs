using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LocalizedGlossConfiguration : IEntityTypeConfiguration<LocalizedGloss>
{
    public void Configure(EntityTypeBuilder<LocalizedGloss> builder)
    {
        builder.ToTable("localized_glosses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SenseId).HasColumnName("sense_id").IsRequired();
        builder.Property(e => e.LanguageTag).HasColumnName("language_tag").IsRequired();
        builder.Property(e => e.GlossText).HasColumnName("gloss_text").IsRequired();
        builder.Property(e => e.SourceRecordId).HasColumnName("source_record_id");
        builder.Property(e => e.ReviewStatus).HasColumnName("review_status");
        builder.Property(e => e.Position).HasColumnName("position").HasDefaultValue((short)0);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.SenseId, e.LanguageTag }).HasDatabaseName("idx_localized_glosses_sense_lang");

        // FK tới source_records (provenance) — SET NULL on delete
        builder.HasOne(e => e.SourceRecord)
            .WithMany(s => s.Glosses)
            .HasForeignKey(e => e.SourceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        // CHECK constraint cho language_tag qua HasCheckConstraint
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_localized_glosses_language_tag",
            "language_tag IN ('vi', 'en')"));
    }
}
