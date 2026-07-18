using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserCredentialConfiguration : IEntityTypeConfiguration<UserCredential>
    {
        public void Configure(EntityTypeBuilder<UserCredential> builder)
        {
            builder.ToTable("user_credentials");

            // Primary Key
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // Foreign Key
            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // Password
            builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            // Created Date
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            // Unique UserId
            builder.HasIndex(x => x.UserId)
                .IsUnique();

            // Relationship with User
            builder.HasOne(x => x.User)
                .WithOne(x => x.UserCredential)
                .HasForeignKey<UserCredential>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}