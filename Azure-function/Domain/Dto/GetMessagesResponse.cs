namespace Domain.Dto
{
    public class GetMessagesResponse
    {
        public Guid Id { get; set; }

        public Guid ChatRoomId { get; set; }

        public Guid UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// "public" or "private".
        /// Frontend must never render private messages to attendees who are not the sender.
        /// The server-side filter enforces this — this field is informational for the client.
        /// </summary>
        public string MessageType { get; set; } = "public";

        public Guid? ReceiverUserId { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// "attendee" or "speaker".
        /// Allows the frontend to visually distinguish questions from replies.
        /// </summary>
        public string? SenderType { get; set; }
    }
}