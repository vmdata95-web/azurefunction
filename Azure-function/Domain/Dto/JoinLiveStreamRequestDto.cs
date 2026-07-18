using System;

namespace Domain.Dto
{
    /// <summary>
    /// Request body for POST /api/live-stream/join.
    /// </summary>
    public class JoinLiveStreamRequestDto
    {
        /// <summary>The session (live stream) the user wants to join.</summary>
        public Guid SessionId { get; set; }

        /// <summary>The user joining the live stream.</summary>
        public Guid UserId { get; set; }
    }
}
