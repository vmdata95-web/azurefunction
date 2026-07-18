using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
    {
        public void Configure(EntityTypeBuilder<ChatRoom> builder)
        {
            builder.ToTable("chat_rooms");

            // PRIMARY KEY
            builder.HasKey(x => x.Id);

            // ID
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // EVENT ID
            builder.Property(x => x.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            // SESSION ID (nullable FK — null for legacy ChatRooms)
            // When populated: speaker auth uses Session-scoped chain.
            // When null:       speaker auth falls back to Event-scoped chain.
            builder.Property(x => x.SessionId)
                .HasColumnName("session_id")
                .IsRequired(false);

            // TYPE
            builder.Property(x => x.Type)
                .HasColumnName("type")
                .HasMaxLength(50)
                .IsRequired();

            // FOREIGN KEY — ChatRoom → Event
            builder.HasOne(x => x.Event)
                .WithMany(e => e.ChatRooms)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // FOREIGN KEY — ChatRoom → Session (nullable)
            // SetNull: deleting a Session clears SessionId rather than deleting the ChatRoom.
            // This preserves the legacy ChatRoom and allows the event-scoped fallback to activate.
            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        }
    }
}