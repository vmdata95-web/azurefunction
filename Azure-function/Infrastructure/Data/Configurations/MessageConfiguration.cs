using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("messages");

            // PRIMARY KEY
            builder.HasKey(x => x.Id);

            // ID
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // CHAT ROOM ID
            builder.Property(x => x.ChatRoomId)
                .HasColumnName("chat_room_id");

            // USER ID
            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            // USER MESSAGE
            builder.Property(x => x.MessageText)
                .HasColumnName("message")
                .HasColumnType("NVARCHAR(MAX)");

            // SPEAKER REPLY
            builder.Property(x => x.SpeakerReply)
                .HasColumnName("speaker_reply")
                .HasColumnType("NVARCHAR(MAX)");

            // CREATED AT
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME2")
                .HasDefaultValueSql("GETDATE()");

            // REPLIED AT
            builder.Property(x => x.RepliedAt)
                .HasColumnName("replied_at")
                .HasColumnType("DATETIME2");

            // MESSAGE TYPE
            builder.Property(x => x.MessageType)
                .HasColumnName("message_type")
                .HasColumnType("NVARCHAR(20)")
                .HasDefaultValue("public");

            // RECEIVER USER ID
            builder.Property(x => x.ReceiverUserId)
                .HasColumnName("receiver_user_id");

            // REPLY TO MESSAGE ID
            builder.Property(x => x.ReplyToMessageId)
                .HasColumnName("reply_to_message_id");

            // IS DELETED
            builder.Property(x => x.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            // CHAT ROOM RELATION
            builder.HasOne(x => x.ChatRoom)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatRoomId)
                .OnDelete(DeleteBehavior.NoAction);

            // SENDER RELATION
            builder.HasOne(x => x.User)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // RECEIVER RELATION
            builder.HasOne(x => x.ReceiverUser)
                .WithMany()
                .HasForeignKey(x => x.ReceiverUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // REPLY RELATION
            builder.HasOne(x => x.ReplyToMessage)
                .WithMany()
                .HasForeignKey(x => x.ReplyToMessageId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── New conversation-architecture columns ───────────────────────────

            // CONVERSATION ID
            builder.Property(x => x.ConversationId)
                .HasColumnName("conversation_id");

            // SOURCE MESSAGE ID
            builder.Property(x => x.SourceMessageId)
                .HasColumnName("source_message_id");

            // SENDER TYPE
            builder.Property(x => x.SenderType)
                .HasColumnName("sender_type")
                .HasMaxLength(20);

            // CONVERSATION RELATION
            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            // SOURCE MESSAGE SELF-REFERENCE
            builder.HasOne(x => x.SourceMessage)
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}