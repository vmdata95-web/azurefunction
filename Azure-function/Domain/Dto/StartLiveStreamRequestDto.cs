using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    /// <summary>
    /// Request body received from the client to start a live stream.
    /// Mapped to StartLiveStreamCommand by the controller.
    /// </summary>
    public class StartLiveStreamRequestDto
    {
        /// <summary>The session that should go live.</summary>
        public Guid SessionId { get; set; }

        /// <summary>The room in which the session is held.</summary>
        public Guid RoomId { get; set; }

        /// <summary>The speaker starting the stream.</summary>
        public Guid SpeakerId { get; set; }
    }
}
