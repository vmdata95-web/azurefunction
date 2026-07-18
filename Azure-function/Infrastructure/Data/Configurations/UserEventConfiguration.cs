using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
    {
        public void Configure(EntityTypeBuilder<UserEvent> builder)
        {
            builder.ToTable("user_events");

            // ✅ Primary Key
            builder.HasKey(x => x.Id);

            // ✅ Columns mapping (snake_case)
            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();   // nullable

            builder.Property(x => x.EventId)
                .HasColumnName("event_id")
                .IsRequired(false);   // nullable

            builder.Property(x => x.RegisteredAt)
                .HasColumnName("registered_at")
                .HasDefaultValueSql("GETDATE()") // DB default
                .IsRequired(false);

            builder.Property(x => x.IsCheckedIn)
                .HasColumnName("is_checked_in")
                .IsRequired(false);

            // ✅ Relationships

            builder.HasOne(x => x.User)
                .WithMany(u => u.UserEvents)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict); // 🔥 safe

            builder.HasOne(x => x.Event)
                .WithMany(e => e.UserEvents)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Restrict); // 🔥 safe
        }
    }
}