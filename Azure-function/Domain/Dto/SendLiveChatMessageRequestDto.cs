using System;

namespace Domain.Dto
{
    /// <summary>
    /// Request body for POST /api/live-stream/chat/send.
    /// </summary>
    public class SendLiveChatMessageRequestDto
    {
        /// <summary>The session (live stream room) to post the message in.</summary>
        public Guid   SessionId   { get; set; }

        /// <summary>The user sending the message.</summary>
        public Guid   UserId      { get; set; }

        /// <summary>The chat message text. Must not be empty.</summary>
        public string MessageText { get; set; } = string.Empty;
    }
}
