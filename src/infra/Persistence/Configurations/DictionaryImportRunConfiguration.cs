using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DictionaryImportRunConfiguration : IEntityTypeConfiguration<DictionaryImportRun>
{
    public void Configure(EntityTypeBuilder<DictionaryImportRun> builder)
    {
        builder.ToTable("dictionary_import_runs", table => table.HasCheckConstraint("ck_import_state",
            "state IN ('new','loading','failed','cancelled','imported','validated','approved_for_local_test','sealed')"));
        builder.HasKey(x => x.RunId);
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.TargetDatabase).HasColumnName("target_database").IsRequired();
        builder.Property(x => x.DatabaseOid).HasColumnName("database_oid");
        builder.Property(x => x.SourceLockHash).HasColumnName("source_lock_hash").IsRequired();
        builder.Property(x => x.FingerprintHash).HasColumnName("fingerprint_hash").IsRequired();
        builder.Property(x => x.State).HasColumnName("state").IsRequired();
        builder.Property(x => x.FactsHash).HasColumnName("facts_hash");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.HasIndex(x => x.TargetDatabase).IsUnique();
    }
}

public sealed class DictionaryImportCheckpointConfiguration : IEntityTypeConfiguration<DictionaryImportCheckpoint>
{
    public void Configure(EntityTypeBuilder<DictionaryImportCheckpoint> builder)
    {
        builder.ToTable("dictionary_import_checkpoints", table =>
            table.HasCheckConstraint("ck_import_checkpoint_count", "accepted_records >= 0"));
        builder.HasKey(x => new { x.RunId, x.SourceId });
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.SourceId).HasColumnName("source_id");
        builder.Property(x => x.ManifestId).HasColumnName("manifest_id");
        builder.Property(x => x.AcceptedRecords).HasColumnName("accepted_records");
        builder.Property(x => x.LastSourceIdentity).HasColumnName("last_source_identity").IsRequired();
        builder.Property(x => x.BatchChainHash).HasColumnName("batch_chain_hash").IsRequired();
        builder.HasOne(x => x.Run).WithMany(x => x.Checkpoints).HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Manifest).WithMany().HasForeignKey(x => x.ManifestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ManifestId).IsUnique();
    }
}
