using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SpeakerConfiguration : IEntityTypeConfiguration<Speaker>
    {
        public void Configure(EntityTypeBuilder<Speaker> builder)
        {
            // ✅ Table
            builder.ToTable("speakers");

            // ✅ Primary Key
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // ✅ UserId (FK)
            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // ✅ Bio
            builder.Property(x => x.Bio)
                .HasColumnName("bio");

            // ✅ Company
            builder.Property(x => x.Company)
                .HasColumnName("company")
                .HasMaxLength(150);

            // ✅ Website
            builder.Property(x => x.Website)
                .HasColumnName("website");

            // ✅ CreatedAt
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            // 🔥 Relationship (User ↔ Speaker)
            builder.HasOne(x => x.User)
                .WithOne(u => u.Speaker)
                .HasForeignKey<Speaker>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade); // important
        }
    }
}