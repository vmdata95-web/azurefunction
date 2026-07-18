using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            // ✅ Table
            builder.ToTable("sessions");

            // ✅ Primary Key
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // ✅ RoomId (FK)
            builder.Property(x => x.RoomId)
                .HasColumnName("room_id")
                .IsRequired();

            // ✅ SpeakerId (FK)
            builder.Property(x => x.SpeakerId)
                .HasColumnName("speaker_id")
                .IsRequired();

            // ✅ Title
            builder.Property(x => x.Title)
                .HasColumnName("title")
                .IsRequired();

            // ✅ StartTime
            builder.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            // ✅ EndTime
            builder.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .IsRequired();

            // ✅ VideoUrl
            builder.Property(x => x.VideoUrl)
                .HasColumnName("video_url");

            // ✅ Status
            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(50);

            // ✅ TotalHlsSegments (nullable)
            builder.Property(x => x.TotalHlsSegments)
                .HasColumnName("TotalHlsSegments")
                .IsRequired(false);

            // 🔥 Relationship: Room → Sessions
            builder.HasOne(x => x.Room)
                .WithMany(r => r.Sessions)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔥 Relationship: Speaker → Sessions 
            builder.HasOne(x => x.Speaker)
                .WithMany(s => s.Sessions)
                .HasForeignKey(x => x.SpeakerId)
                .OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}