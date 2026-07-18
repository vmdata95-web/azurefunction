using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    /// <summary>
    /// Read-model returned by ILiveStreamRepository queries.
    /// Contains flattened, projection-safe data (no EF navigation properties).
    /// </summary>
    public class LiveStreamDto
    {
        public Guid   LiveStreamId { get; set; }
        public Guid   SessionId    { get; set; }
        public Guid   RoomId       { get; set; }
        public Guid   SpeakerId    { get; set; }

        public string SessionTitle { get; set; } = string.Empty;
        public string RoomName     { get; set; } = string.Empty;

        /// <summary>String representation of <see cref="Domain.Enums.LiveStreamStatus"/>.</summary>
        public string Status       { get; set; } = string.Empty;

        public string StreamKey    { get; set; } = string.Empty;

        public DateTime? StartedAt  { get; set; }
        public DateTime? EndedAt    { get; set; }
        public DateTime  CreatedAt  { get; set; }
    }
}
