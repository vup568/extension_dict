using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CurrentKnowledgeReleaseConfiguration : IEntityTypeConfiguration<CurrentKnowledgeRelease>
{
    public void Configure(EntityTypeBuilder<CurrentKnowledgeRelease> builder)
    {
        builder.ToTable("current_knowledge_release", table => table.HasCheckConstraint(
            "chk_current_knowledge_release_singleton",
            "singleton_id = 1"));

        builder.HasKey(e => e.SingletonId);
        builder.Property(e => e.SingletonId).HasColumnName("singleton_id").ValueGeneratedNever();
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").IsRequired();

        builder.HasIndex(e => e.ReleaseId)
            .IsUnique()
            .HasDatabaseName("uq_current_knowledge_release_release");

        builder.HasOne(e => e.Release)
            .WithOne()
            .HasForeignKey<CurrentKnowledgeRelease>(e => e.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
