using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class LiveStreamConfiguration : IEntityTypeConfiguration<LiveStream>
    {
        public void Configure(EntityTypeBuilder<LiveStream> builder)
        {
            builder.ToTable("live_streams");

            // PRIMARY KEY
            builder.HasKey(x => x.Id);

            // ID
            builder.Property(x => x.Id)
                .HasColumnName("id");

            // SESSION ID
            builder.Property(x => x.SessionId)
                .HasColumnName("session_id");

            // ROOM ID
            builder.Property(x => x.RoomId)
                .HasColumnName("room_id");

            // SPEAKER ID
            builder.Property(x => x.SpeakerId)
                .HasColumnName("speaker_id");

            // STREAM KEY
            builder.Property(x => x.StreamKey)
                .HasColumnName("stream_key")
                .HasMaxLength(200);

            // STATUS -> is_live
            builder.Property(x => x.Status)
                .HasColumnName("is_live")
                .HasConversion(
                    v => v == LiveStreamStatus.Live,
                    v => v
                        ? LiveStreamStatus.Live
                        : LiveStreamStatus.Ended
                );

            // STARTED AT
            builder.Property(x => x.StartedAt)
                .HasColumnName("started_at");

            // ENDED AT
            builder.Property(x => x.EndedAt)
                .HasColumnName("ended_at");

            // IGNORE NON-EXISTING COLUMNS
            builder.Ignore(x => x.Metadata);
            builder.Ignore(x => x.CreatedAt);
            builder.Ignore(x => x.UpdatedAt);

            // RELATIONSHIPS

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Room)
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Speaker)
                .WithMany()
                .HasForeignKey(x => x.SpeakerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}