using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext chính cho JP Reading Platform V2.
/// Quản lý toàn bộ 17 bảng PostgreSQL 16 theo ARCH-005, ARCH-007.
/// Fluent API configuration được tách ra từng file IEntityTypeConfiguration riêng.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Knowledge Core
    public DbSet<DictionaryEntry> DictionaryEntries => Set<DictionaryEntry>();
    public DbSet<WrittenForm> WrittenForms => Set<WrittenForm>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<DictionarySense> DictionarySenses => Set<DictionarySense>();
    public DbSet<LocalizedGloss> LocalizedGlosses => Set<LocalizedGloss>();
    public DbSet<SenseApplicability> SenseApplicabilities => Set<SenseApplicability>();

    // Kanji
    public DbSet<KanjiRecord> KanjiRecords => Set<KanjiRecord>();

    // Grammar
    public DbSet<GrammarRule> GrammarRules => Set<GrammarRule>();

    // Knowledge Operations
    public DbSet<SourceManifest> SourceManifests => Set<SourceManifest>();
    public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();
    public DbSet<EditorialMapping> EditorialMappings => Set<EditorialMapping>();
    public DbSet<KnowledgeRelease> KnowledgeReleases => Set<KnowledgeRelease>();
    public DbSet<KnowledgeReleaseManifest> KnowledgeReleaseManifests => Set<KnowledgeReleaseManifest>();
    public DbSet<ResourceRevision> ResourceRevisions => Set<ResourceRevision>();

    // User & Learning
    public DbSet<User> Users => Set<User>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<LearningReference> LearningReferences => Set<LearningReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tự động load tất cả IEntityTypeConfiguration<T> trong assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
