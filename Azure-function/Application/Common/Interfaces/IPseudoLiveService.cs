using System;

namespace Application.Common.Interfaces
{
    public interface IPseudoLiveService
    {
        int GetCurrentSegmentIndex(DateTime sessionStartTime, int segmentDurationSeconds);

        /// <summary>
        /// Caps the current segment index to the total available segments.
        /// </summary>
        int GetClampedCurrentIndex(int currentIndex, int? totalSegments);

        bool IsSegmentAllowed(int requestedIndex, int currentIndex, int windowSize);

        /// <summary>
        /// Determines if a segment is allowed, with VOD fallback logic.
        /// Allows full VOD range once playback reaches the final segment.
        /// </summary>
        bool IsSegmentAllowedWithVod(int requestedIndex, int currentIndex, int windowSize, int? totalSegments);

        string GenerateLivePlaylist(
            Guid sessionId,
            int currentIndex,
            int windowSize,
            int segmentDurationSeconds,
            string baseSegmentUrl);

        string GenerateLivePlaylist(
            Guid sessionId,
            int currentIndex,
            int windowSize,
            int segmentDurationSeconds,
            string baseSegmentUrl,
            int maxAllowedIndex);

        int ParseSegmentCount(string playlistContent);

        // ── VOD / Recorded ──────────────────────────────────────────────────────────
        /// <summary>
        /// Generates a complete HLS Media Playlist suitable for Video-on-Demand (VOD) playback.
        /// Includes every segment, <c>#EXT-X-PLAYLIST-TYPE:VOD</c>, and <c>#EXT-X-ENDLIST</c>
        /// so that HLS players expose the full seek bar and allow random access.
        /// </summary>
        /// <param name="sessionId">The session GUID.</param>
        /// <param name="totalSegments">Total number of segments in the recording.</param>
        /// <param name="segmentDurationSeconds">Duration of each segment in seconds.</param>
        /// <param name="baseSegmentUrl">Base URL for segment proxy endpoints.</param>
        /// <returns>A valid HLS VOD Media Playlist string.</returns>
        string GenerateVodPlaylist(
            Guid sessionId,
            int totalSegments,
            int segmentDurationSeconds,
            string baseSegmentUrl);
    }
}
