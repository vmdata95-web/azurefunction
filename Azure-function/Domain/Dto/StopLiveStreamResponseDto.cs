using System;

namespace Domain.Dto
{
    /// <summary>
    /// Response body returned after successfully stopping a live stream.
    /// </summary>
    public class StopLiveStreamResponseDto
    {
        public bool   Success    { get; set; } = true;
        public string Message    { get; set; } = "Live stream stopped successfully.";

        public Guid   SessionId  { get; set; }
        public Guid   LiveStreamId { get; set; }

        /// <summary>UTC timestamp the stream ended.</summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>Final status string, e.g. "Ended".</summary>
        public string Status     { get; set; } = string.Empty;
    }
}
