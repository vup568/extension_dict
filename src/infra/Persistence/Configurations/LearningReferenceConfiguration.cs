using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LearningReferenceConfiguration : IEntityTypeConfiguration<LearningReference>
{
    public void Configure(EntityTypeBuilder<LearningReference> builder)
    {
        builder.ToTable("learning_references");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.ResourceType).HasColumnName("resource_type").IsRequired();
        builder.Property(e => e.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(e => e.SavedAt).HasColumnName("saved_at").HasDefaultValueSql("now()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        // Partial unique: không cho duplicate active items (LEARN-007)
        builder.HasIndex(e => new { e.UserId, e.ResourceType, e.ResourceId })
            .IsUnique()
            .HasDatabaseName("uq_learning_references_active")
            .HasFilter("deleted_at IS NULL");

        // Fast active-only queries (LEARN-008)
        builder.HasIndex(e => new { e.UserId, e.ResourceType })
            .HasDatabaseName("idx_learning_references_user_active")
            .HasFilter("deleted_at IS NULL");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_learning_references_type",
            "resource_type IN ('dictionary', 'grammar')"));
    }
}
