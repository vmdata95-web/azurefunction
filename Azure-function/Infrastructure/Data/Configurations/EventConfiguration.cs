using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("events");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .IsRequired(false);

            builder.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .IsRequired(false);

            builder.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .IsRequired(false);

            builder.Property(x => x.BannerUrl)
                .HasColumnName("banner_url")
                .IsRequired(false);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(false)
                .IsRequired(false);
        }
    }
}