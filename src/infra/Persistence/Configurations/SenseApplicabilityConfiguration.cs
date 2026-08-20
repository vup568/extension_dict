using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SenseApplicabilityConfiguration : IEntityTypeConfiguration<SenseApplicability>
{
    public void Configure(EntityTypeBuilder<SenseApplicability> builder)
    {
        builder.ToTable("sense_applicabilities");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SenseId).HasColumnName("sense_id").IsRequired();
        builder.Property(e => e.WrittenFormId).HasColumnName("written_form_id");
        builder.Property(e => e.ReadingId).HasColumnName("reading_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        // FKs — CASCADE on delete
        builder.HasOne(e => e.WrittenForm)
            .WithMany(f => f.SenseApplicabilities)
            .HasForeignKey(e => e.WrittenFormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ReadingEntity)
            .WithMany(r => r.SenseApplicabilities)
            .HasForeignKey(e => e.ReadingId)
            .OnDelete(DeleteBehavior.Cascade);

        // CHECK: ít nhất một target phải NOT NULL
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_sense_applicabilities_has_target",
            "written_form_id IS NOT NULL OR reading_id IS NOT NULL"));

        // Unique combo index (dùng COALESCE để handle NULL)
        builder.HasIndex(e => new { e.SenseId, e.WrittenFormId, e.ReadingId })
            .IsUnique()
            .HasDatabaseName("uq_sense_applicabilities_combo");
    }
}
