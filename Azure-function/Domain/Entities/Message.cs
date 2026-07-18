namespace Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ChatRoomId { get; set; }

        public Guid UserId { get; set; }

        public string MessageText { get; set; } = string.Empty;
        public string? SpeakerReply { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RepliedAt { get; set; }

        public string MessageType { get; set; } = "public";

        public Guid? ReceiverUserId { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        public bool IsDeleted { get; set; } = false;

        // ── New conversation-architecture columns ───────────────────────────
        public Guid? ConversationId { get; set; }

        public Guid? SourceMessageId { get; set; }

        public string? SenderType { get; set; }

        // Navigation
        public virtual ChatRoom? ChatRoom { get; set; }

        public virtual User? User { get; set; }

        public virtual User? ReceiverUser { get; set; }

        public virtual Message? ReplyToMessage { get; set; }

        public virtual ChatConversation? Conversation { get; set; }

        public virtual Message? SourceMessage { get; set; }
    }
}