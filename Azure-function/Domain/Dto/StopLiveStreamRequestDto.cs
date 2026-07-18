using System;

namespace Domain.Dto
{
    /// <summary>
    /// Request body for POST /api/live-stream/stop.
    /// Identifies the live stream to be stopped by its session.
    /// </summary>
    public class StopLiveStreamRequestDto
    {
        /// <summary>The session whose active live stream should be stopped.</summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// The speaker requesting the stop — must match the stream owner.
        /// </summary>
        public Guid SpeakerId { get; set; }
    }
}
