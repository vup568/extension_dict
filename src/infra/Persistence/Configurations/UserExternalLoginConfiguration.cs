using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Provider).HasColumnName("provider").IsRequired();
        builder.Property(e => e.ProviderUserId).HasColumnName("provider_user_id").IsRequired();
        builder.Property(e => e.ProviderEmail).HasColumnName("provider_email");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        // Không cho 2 account cùng Google ID
        builder.HasIndex(e => new { e.Provider, e.ProviderUserId })
            .IsUnique().HasDatabaseName("uq_user_external_logins_provider_uid");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_user_external_logins_user_id");
    }
}
