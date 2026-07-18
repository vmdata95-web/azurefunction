using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
    {
        public void Configure(EntityTypeBuilder<UserActivityLog> builder)
        {
            builder.ToTable("user_activity_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            builder.Property(x => x.EventId)
                .HasColumnName("event_id");

            builder.Property(x => x.Action)
                .HasColumnName("action")
                .HasConversion<string>()
                .HasMaxLength(100);

            builder.Property(x => x.RoomName)
                .HasColumnName("room_name")
                .HasMaxLength(255);

            builder.Property(x => x.Metadata)
                .HasColumnName("metadata");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne(x => x.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Event)
                .WithMany(e => e.ActivityLogs)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_user_activity_logs_metadata",
                    "ISJSON([metadata]) > 0"
                );
            });
        }
    }
}