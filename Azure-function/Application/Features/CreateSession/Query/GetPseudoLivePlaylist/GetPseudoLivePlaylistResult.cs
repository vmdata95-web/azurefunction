using System.Collections.Generic;

namespace Application.Features.CreateSession.Query.GetPseudoLivePlaylist
{
    /// <summary>
    /// Result of a playlist request.
    /// <para>
    ///   If <see cref="PlaylistContent"/> is set, the caller should stream it as
    ///   <c>application/vnd.apple.mpegurl</c>.
    /// </para>
    /// <para>
    ///   If <see cref="AvailablePlaylists"/> is set (and <see cref="PlaylistContent"/> is null),
    ///   multiple playlists exist for the session and the caller should return the list as JSON
    ///   so the client can select one.
    /// </para>
    /// </summary>
    public class GetPseudoLivePlaylistResult
    {
        /// <summary>
        /// The rewritten .m3u8 content ready for streaming to the HLS player.
        /// Null when multiple playlists exist and none was selected.
        /// </summary>
        public string? PlaylistContent { get; set; }

        /// <summary>
        /// List of available playlist file names when the session has more than one playlist
        /// and no specific playlist was requested.
        /// </summary>
        public List<string>? AvailablePlaylists { get; set; }

        /// <summary>
        /// True when the result is a direct playlist; false when it is a menu of choices.
        /// </summary>
        public bool IsPlaylist => PlaylistContent != null;
    }
}
