using System;

namespace Domain.Dto
{
    /// <summary>
    /// Response body returned after a user successfully joins a live stream.
    /// </summary>
    public class JoinLiveStreamResponseDto
    {
        public bool   Success      { get; set; } = true;
        public string Message      { get; set; } = "Joined live stream successfully.";

        public Guid   SessionId    { get; set; }
        public Guid   UserId       { get; set; }

        /// <summary>Stream key the viewer can use to connect to the CDN/WebRTC endpoint.</summary>
        public string StreamKey    { get; set; } = string.Empty;

        public string RoomName     { get; set; } = string.Empty;
        public string SessionTitle { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the join was recorded.</summary>
        public DateTime JoinedAt   { get; set; }
    }
}
