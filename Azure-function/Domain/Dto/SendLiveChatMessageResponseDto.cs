using System;

namespace Domain.Dto
{
    /// <summary>
    /// Response body returned after a live chat message is successfully sent.
    /// </summary>
    public class SendLiveChatMessageResponseDto
    {
        public bool   Success     { get; set; } = true;
        public string Message     { get; set; } = "Message sent successfully.";

        /// <summary>The newly created message's database id.</summary>
        public Guid   MessageId   { get; set; }

        public Guid   SessionId   { get; set; }
        public Guid   UserId      { get; set; }

        public string MessageText { get; set; } = string.Empty;

        /// <summary>UTC timestamp the message was persisted.</summary>
        public DateTime SentAt    { get; set; }
    }
}
