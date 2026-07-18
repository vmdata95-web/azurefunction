using System;

namespace Domain.Dto
{
    /// <summary>
    /// Response body returned after a user successfully leaves a live stream.
    /// </summary>
    public class LeaveLiveStreamResponseDto
    {
        public bool     Success   { get; set; } = true;
        public string   Message   { get; set; } = "Left live stream successfully.";

        public Guid     SessionId { get; set; }
        public Guid     UserId    { get; set; }

        /// <summary>UTC timestamp when the leave was recorded.</summary>
        public DateTime LeftAt    { get; set; }
    }
}
