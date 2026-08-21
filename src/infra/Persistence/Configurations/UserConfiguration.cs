using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Email).HasColumnName("email").IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name");
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash");
        builder.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_users_email");

        builder.HasMany(e => e.ExternalLogins).WithOne(l => l.User).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.LearningReferences).WithOne(r => r.User).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
