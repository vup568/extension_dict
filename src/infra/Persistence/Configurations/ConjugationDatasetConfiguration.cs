using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ConjugationDatasetConfiguration : IEntityTypeConfiguration<ConjugationDataset>
{
    public void Configure(EntityTypeBuilder<ConjugationDataset> builder)
    {
        builder.ToTable("conjugation_datasets");
        builder.HasKey(x => x.Version);
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb").IsRequired();
    }
}
