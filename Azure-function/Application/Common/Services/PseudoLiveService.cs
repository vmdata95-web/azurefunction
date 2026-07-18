using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Application.Common.Services
{
    public sealed class PseudoLiveService : IPseudoLiveService
    {
        private readonly ITimeProvider _timeProvider;
        private readonly ILogger<PseudoLiveService> _logger;

        public PseudoLiveService(
            ITimeProvider timeProvider,
            ILogger<PseudoLiveService> logger)
        {
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public int GetCurrentSegmentIndex(DateTime eventStartTime, int segmentDurationSeconds)
        {
            if (segmentDurationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(segmentDurationSeconds),
                    "Segment duration must be a positive integer.");

            var now = _timeProvider.GetLocalTime();
            var elapsed = now - eventStartTime;

            // If the event hasn't started yet (clock skew / tiny race), treat as 0.
            if (elapsed.TotalSeconds < 0)
            {
                _logger.LogWarning(
                    "[PseudoLive] GetCurrentSegmentIndex called before event start. " +
                    "EventStart={EventStart}, Now={Now}. Returning index 0.",
                    eventStartTime, now);
                return 0;
            }

            var index = (int)(elapsed.TotalSeconds / segmentDurationSeconds);

            _logger.LogDebug(
                "[PseudoLive] Elapsed={Elapsed:F1}s, SegmentDuration={Dur}s → CurrentIndex={Index}",
                elapsed.TotalSeconds, segmentDurationSeconds, index);

            return index;
        }

        /// <summary>
        /// Caps the current segment index to the total available segments.
        /// Used to prevent index overflow when elapsed time exceeds the video duration.
        /// </summary>
        /// <param name="currentIndex">The theoretical segment index based on elapsed time.</param>
        /// <param name="totalSegments">Total number of segments available (nullable for legacy sessions).</param>
        /// <returns>The clamped segment index, or the original index if total is unknown.</returns>
        public int GetClampedCurrentIndex(int currentIndex, int? totalSegments)
        {
            if (totalSegments is null)
            {
                _logger.LogDebug(
                    "[PseudoLive] GetClampedCurrentIndex: totalSegments is null, returning unclamped index {Index}",
                    currentIndex);
                return currentIndex;
            }

            // Last valid segment is at index (totalSegments - 1)
            var maxIndex = totalSegments.Value - 1;
            var clamped = Math.Min(currentIndex, maxIndex);

            if (clamped != currentIndex)
            {
                _logger.LogDebug(
                    "[PseudoLive] GetClampedCurrentIndex: clamped {Current} to {Clamped} (total={Total})",
                    currentIndex, clamped, totalSegments);
            }

            return clamped;
        }

        public bool IsSegmentAllowed(int requestedIndex, int currentIndex, int windowSize)
        {
            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize),
                    "Window size must be a positive integer.");

            // Window: [firstAllowed, currentIndex]
            // firstAllowed = max(0, currentIndex - windowSize + 1)
            var firstAllowed = Math.Max(0, currentIndex - windowSize + 1);
            var allowed = requestedIndex >= firstAllowed && requestedIndex <= currentIndex;

            _logger.LogDebug(
                "[PseudoLive] SegmentCheck: requested={Req}, current={Cur}, " +
                "window=[{First},{Cur}] → {Result}",
                requestedIndex, currentIndex, firstAllowed, currentIndex,
                allowed ? "ALLOWED" : "DENIED");

            return allowed;
        }

        /// <summary>
        /// Determines if a segment request is allowed, with VOD fallback.
        /// 
        /// During live playback (currentIndex &lt; totalSegments):
        ///   Uses sliding window: [currentIndex - windowSize + 1, currentIndex]
        ///   Prevents replay attacks and out-of-order seeks.
        /// 
        /// After playback completes (currentIndex >= totalSegments - 1):
        ///   Allows full VOD range: [0, totalSegments - 1]
        ///   Permits catch-up and delayed joining.
        /// </summary>
        /// <param name="requestedIndex">Zero-based index of the requested segment.</param>
        /// <param name="currentIndex">Current playback position (already clamped to total).</param>
        /// <param name="windowSize">Sliding window size during live playback.</param>
        /// <param name="totalSegments">Total number of segments (nullable for legacy sessions).</param>
        /// <returns>True if the segment is permitted; false if it falls outside policy.</returns>
        public bool IsSegmentAllowedWithVod(int requestedIndex, int currentIndex, int windowSize, int? totalSegments)
        {
            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize),
                    "Window size must be a positive integer.");

            if (requestedIndex < 0)
            {
                _logger.LogDebug("[PseudoLive] IsSegmentAllowedWithVod: requested index is negative");
                return false;
            }

            // If total segments is unknown (legacy), fall back to sliding window only
            if (totalSegments is null)
            {
                var firstAllowed = Math.Max(0, currentIndex - windowSize + 1);
                var allowed = requestedIndex >= firstAllowed && requestedIndex <= currentIndex;
                _logger.LogDebug(
                    "[PseudoLive] IsSegmentAllowedWithVod (legacy, no total): " +
                    "requested={Req}, current={Cur}, window=[{First},{Cur}] → {Result}",
                    requestedIndex, currentIndex, firstAllowed, currentIndex, allowed ? "ALLOWED" : "DENIED");
                return allowed;
            }

            var maxSegmentIndex = totalSegments.Value - 1;

            // Bound check: requested index cannot exceed available segments
            if (requestedIndex > maxSegmentIndex)
            {
                _logger.LogWarning(
                    "[PseudoLive] IsSegmentAllowedWithVod: requested={Req} exceeds max={Max}",
                    requestedIndex, maxSegmentIndex);
                return false;
            }

            // VOD phase: if current index is at or past the last segment, allow full range
            if (currentIndex >= maxSegmentIndex)
            {
                _logger.LogDebug(
                    "[PseudoLive] IsSegmentAllowedWithVod: VOD phase (current={Cur} >= max={Max}). " +
                    "Allowing full range [0,{Max}] for requested={Req}",
                    currentIndex, maxSegmentIndex, requestedIndex);
                return true;
            }

            // Live phase: enforce sliding window
            var firstAllowedLive = Math.Max(0, currentIndex - windowSize + 1);
            var allowedLive = requestedIndex >= firstAllowedLive && requestedIndex <= currentIndex;

            _logger.LogDebug(
                "[PseudoLive] IsSegmentAllowedWithVod: Live phase. " +
                "Requested={Req}, current={Cur}, window=[{First},{Cur}] → {Result}",
                requestedIndex, currentIndex, firstAllowedLive, currentIndex, 
                allowedLive ? "ALLOWED" : "DENIED");

            return allowedLive;
        }

        public string GenerateLivePlaylist(
                Guid sessionId,
                int currentIndex,
                int windowSize,
                int segmentDurationSeconds,
                string baseSegmentUrl)
            {
                return GenerateLivePlaylist(
                    sessionId,
                    currentIndex,
                    windowSize,
                    segmentDurationSeconds,
                    baseSegmentUrl,
                    maxAllowedIndex: int.MaxValue);
            }

            /// <summary>
            /// Generates a pseudo-live HLS Media Playlist that includes only a sliding window of segments.
            /// </summary>
            /// <param name="sessionId">The session GUID.</param>
            /// <param name="currentIndex">The current playback segment index (based on server time).</param>
            /// <param name="windowSize">Number of segments to include in the window (e.g., 5).</param>
            /// <param name="segmentDurationSeconds">Duration of each segment in seconds.</param>
            /// <param name="baseSegmentUrl">Base URL for segment endpoints (e.g., https://api.example.com/api/Exhibitor/pseudo-live/segment).</param>
            /// <param name="maxAllowedIndex">Maximum segment index available in the recording (prevents requesting beyond the end).</param>
            /// <returns>A valid HLS Media Playlist string formatted per RFC 8216.</returns>
            public string GenerateLivePlaylist(
                Guid sessionId,
                int currentIndex,
                int windowSize,
                int segmentDurationSeconds,
                string baseSegmentUrl,
                int maxAllowedIndex)
            {
                if (string.IsNullOrWhiteSpace(baseSegmentUrl))
                    throw new ArgumentNullException(nameof(baseSegmentUrl));

                if (windowSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be positive.");

                if (segmentDurationSeconds <= 0)
                    throw new ArgumentOutOfRangeException(nameof(segmentDurationSeconds), "Segment duration must be positive.");

                if (maxAllowedIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(maxAllowedIndex), "Max allowed index cannot be negative.");

                // Clamp currentIndex to available segments
                var clampedCurrentIndex = Math.Min(currentIndex, maxAllowedIndex);

                // Compute the starting segment of this window
                var firstSegment = Math.Max(0, clampedCurrentIndex - windowSize + 1);

                _logger.LogInformation(
                    "[PseudoLive] Generating playlist for sessionId={SessionId}. " +
                    "CurrentIndex={Current} (max={Max}), Window=[{First},{Last}], SegmentDuration={Dur}s",
                    sessionId, clampedCurrentIndex, maxAllowedIndex, firstSegment, clampedCurrentIndex, segmentDurationSeconds);

                /*
                 * HLS Media Playlist spec (RFC 8216):
                 *
                 * #EXTM3U                            — required header tag
                 * #EXT-X-VERSION:3                   — version 3 (EXTINF decimal durations)
                 * #EXT-X-TARGETDURATION:<N>          — max duration of any segment in this playlist
                 * #EXT-X-MEDIA-SEQUENCE:<N>          — sequence number of FIRST segment listed
                 * #EXT-X-ALLOW-CACHE:NO              — instruct clients not to cache segments
                 *
                 * For each segment:
                 *   #EXTINF:<duration.000>,          — exact duration (trailing comma required)
                 *   <segment-url>                    — backend-proxied URL (never Azure direct)
                 *
                 * NO #EXT-X-ENDLIST → player treats this as a LIVE stream and keeps polling
                 */
                var sb = new StringBuilder(512);
                sb.AppendLine("#EXTM3U");
                sb.AppendLine("#EXT-X-VERSION:3");
                sb.AppendLine($"#EXT-X-TARGETDURATION:{segmentDurationSeconds}");
                sb.AppendLine($"#EXT-X-MEDIA-SEQUENCE:{firstSegment}");
                sb.AppendLine("#EXT-X-ALLOW-CACHE:NO");
                sb.AppendLine();

                // Normalise base URL — strip trailing slash so we can always append /sessionId/index
                var baseUrl = baseSegmentUrl.TrimEnd('/');

                for (var i = firstSegment; i <= clampedCurrentIndex; i++)
                {
                    // EXTINF duration — use the configured value with 3 decimal places
                    sb.AppendLine($"#EXTINF:{segmentDurationSeconds}.000,");
                    // Segment URL points to the backend proxy endpoint, never Azure directly
                    sb.AppendLine($"{baseUrl}/{sessionId}/{i}");
                }

                // Intentionally omit #EXT-X-ENDLIST to keep the "live" illusion.
                // The player will re-request this playlist every few seconds.

                return sb.ToString();
            }

            /// <summary>
            /// Parses a VOD playlist content to determine the total number of segments.
            /// Counts lines that end with ".ts" (case-insensitive) to determine segment count.
            /// </summary>
            /// <param name="playlistContent">Raw HLS playlist content.</param>
            /// <returns>Total number of segments found in the playlist.</returns>
            public int ParseSegmentCount(string playlistContent)
            {
                if (string.IsNullOrEmpty(playlistContent))
                    return 0;

                var count = 0;
                var lines = playlistContent.Split(new[] { '\n' }, StringSplitOptions.None);

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    // Count segment references (lines ending with .ts)
                    if (line.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                    }
                }

                _logger.LogDebug("[PseudoLive] Parsed playlist: found {SegmentCount} segments.", count);

                return count;
            }

            // ── VOD / Recorded ───────────────────────────────────────────────────────
            /// <summary>
            /// Generates a complete HLS Media Playlist suitable for Video-on-Demand (VOD) playback.
            /// <para>
            ///   Unlike the pseudo-live sliding-window playlist, this method emits every segment
            ///   from index 0 to <paramref name="totalSegments"/> - 1 and appends
            ///   <c>#EXT-X-ENDLIST</c>, which tells HLS players that the stream is complete and
            ///   allows the player to expose the full seek bar for random access.
            /// </para>
            /// </summary>
            /// <param name="sessionId">The session GUID.</param>
            /// <param name="totalSegments">Total number of segments in the recording.</param>
            /// <param name="segmentDurationSeconds">Duration of each segment in seconds.</param>
            /// <param name="baseSegmentUrl">Base URL for the backend segment proxy endpoints.</param>
            /// <returns>A valid HLS VOD Media Playlist string formatted per RFC 8216.</returns>
            public string GenerateVodPlaylist(
                Guid sessionId,
                int totalSegments,
                int segmentDurationSeconds,
                string baseSegmentUrl)
            {
                if (string.IsNullOrWhiteSpace(baseSegmentUrl))
                    throw new ArgumentNullException(nameof(baseSegmentUrl));

                if (totalSegments <= 0)
                    throw new ArgumentOutOfRangeException(nameof(totalSegments), "Total segments must be a positive integer.");

                if (segmentDurationSeconds <= 0)
                    throw new ArgumentOutOfRangeException(nameof(segmentDurationSeconds), "Segment duration must be positive.");

                _logger.LogInformation(
                    "[PseudoLive:VOD] Generating VOD playlist for sessionId={SessionId}. " +
                    "TotalSegments={Total}, SegmentDuration={Dur}s",
                    sessionId, totalSegments, segmentDurationSeconds);

                /*
                 * HLS VOD Media Playlist (RFC 8216):
                 *
                 * #EXTM3U                            — required header tag
                 * #EXT-X-VERSION:3                   — version 3 (EXTINF decimal durations)
                 * #EXT-X-PLAYLIST-TYPE:VOD           — marks this as a VOD stream (immutable)
                 * #EXT-X-TARGETDURATION:<N>          — max duration of any segment
                 * #EXT-X-MEDIA-SEQUENCE:0            — first segment is always index 0 for VOD
                 * #EXT-X-ALLOW-CACHE:YES             — clients may cache VOD segments
                 *
                 * For each segment (0 to totalSegments-1):
                 *   #EXTINF:<duration.000>,          — exact duration (trailing comma required)
                 *   <segment-url>                    — backend-proxied URL (never Azure direct)
                 *
                 * #EXT-X-ENDLIST                     — signals end of stream → player exposes full seek bar
                 */
                var sb = new StringBuilder(512 + totalSegments * 80);
                sb.AppendLine("#EXTM3U");
                sb.AppendLine("#EXT-X-VERSION:3");
                sb.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
                sb.AppendLine($"#EXT-X-TARGETDURATION:{segmentDurationSeconds}");
                sb.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
                sb.AppendLine("#EXT-X-ALLOW-CACHE:YES");
                sb.AppendLine();

                // Normalise base URL — strip trailing slash
                var baseUrl = baseSegmentUrl.TrimEnd('/');

                // Emit every segment — no sliding window, no current-time calculation
                for (var i = 0; i < totalSegments; i++)
                {
                    sb.AppendLine($"#EXTINF:{segmentDurationSeconds}.000,");
                    sb.AppendLine($"{baseUrl}/{sessionId}/{i}");
                }

                // #EXT-X-ENDLIST marks the stream as complete — this is what enables full seeking
                sb.AppendLine("#EXT-X-ENDLIST");

                return sb.ToString();
            }
    }
}
