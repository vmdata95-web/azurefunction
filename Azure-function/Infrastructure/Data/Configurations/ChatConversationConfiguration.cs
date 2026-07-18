using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
    {
        public void Configure(EntityTypeBuilder<ChatConversation> builder)
        {
            builder.ToTable("chat_conversations");

            // PRIMARY KEY
            builder.HasKey(x => x.Id);

            // ID
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // CHAT ROOM ID
            builder.Property(x => x.ChatRoomId)
                .HasColumnName("chat_room_id")
                .IsRequired();

            // CONVERSATION TYPE
            builder.Property(x => x.ConversationType)
                .HasColumnName("conversation_type")
                .HasMaxLength(20)
                .IsRequired();

            // CHECK CONSTRAINT – enforce allowed values
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_chat_conversations_conversation_type",
                "[conversation_type] IN ('public', 'private')"));

            // USER ID (nullable)
            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            // SPEAKER ID (nullable)
            builder.Property(x => x.SpeakerId)
                .HasColumnName("speaker_id");

            // CREATED AT
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME2")
                .HasDefaultValueSql("GETDATE()");

            // IS ACTIVE
            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            // ── Relationships ─────────────────────────────────────────────────

            // ChatConversation → ChatRoom
            builder.HasOne(x => x.ChatRoom)
                .WithMany()
                .HasForeignKey(x => x.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatConversation → User (attendee)
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // ChatConversation → Speaker
            builder.HasOne(x => x.Speaker)
                .WithMany()
                .HasForeignKey(x => x.SpeakerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
