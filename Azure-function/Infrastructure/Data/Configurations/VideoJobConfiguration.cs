using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class VideoJobConfiguration : IEntityTypeConfiguration<VideoJob>
    {
        public void Configure(EntityTypeBuilder<VideoJob> builder)
        {
            builder.ToTable("VideoJobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id");

            builder.Property(x => x.EventId)
                .HasColumnName("EventId")
                .IsRequired();

            builder.Property(x => x.SessionId)
                .HasColumnName("SessionId");

            builder.Property(x => x.ExhibitorId)
                .HasColumnName("ExhibitorId");

            builder.Property(x => x.RawVideoUrl)
                .HasColumnName("RawVideoUrl")
                .IsRequired();

            builder.Property(x => x.ManifestUrl)
                .HasColumnName("ManifestUrl");

            builder.Property(x => x.AzureFolderPath)
                .HasColumnName("AzureFolderPath")
                .HasMaxLength(500);

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.RetryCount)
                .HasColumnName("RetryCount")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.DurationSeconds)
                .HasColumnName("DurationSeconds");

            builder.Property(x => x.ProcessingStartedAt)
                .HasColumnName("ProcessingStartedAt");

            builder.Property(x => x.ProcessingCompletedAt)
                .HasColumnName("ProcessingCompletedAt");

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("ErrorMessage");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("UpdatedAt");
        }
    }
}
