using System;

namespace Domain.Dto
{
    /// <summary>
    /// Request body for POST /api/live-stream/leave.
    /// </summary>
    public class LeaveLiveStreamRequestDto
    {
        /// <summary>The session the user is leaving.</summary>
        public Guid SessionId { get; set; }

        /// <summary>The user leaving the live stream.</summary>
        public Guid UserId { get; set; }
    }
}
