using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateExhibitor.Query.GetExhibitorVideos
{
    /// <summary>
    /// Response shape returned by GET /api/Exhibitor/Lobby/{eventId}.
    /// The frontend loads <see cref="PlaylistUrl"/> directly into HLS.js —
    /// no Azure Blob URL ever leaves the server.
    /// </summary>
    public class LobbyVideoResponse
    {
        /// <summary>
        /// Relative URL of the pseudo-live HLS playlist for this event,
        /// e.g. "/api/Exhibitor/pseudo-live/playlist/{eventId}".
        /// The client must prepend the API base URL before passing to HLS.js.
        /// </summary>
        public string PlaylistUrl { get; set; } = string.Empty;

        /// <summary>Event start time (IST). Null if not configured.</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>Event end time (IST). Null if not configured.</summary>
        public DateTime? EndTime { get; set; }
    }

}
