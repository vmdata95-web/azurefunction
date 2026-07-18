using Application.Common.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Query.GetPseudoLivePlaylist
{
    public class GetPseudoLivePlaylistQueryHandler
        : IRequestHandler<GetPseudoLivePlaylistQuery, GetPseudoLivePlaylistResult>
    {
        private readonly ISessionValidationService _sessionValidationService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IPseudoLiveService _pseudoLiveService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetPseudoLivePlaylistQueryHandler> _logger;

        // ── NEW: needed to load the session without the active-time gate for Recorded sessions
        private readonly ISessionRepository _sessionRepository;

        private const string LiveVideoFolder = "session-live-video";
        private const string CfgSegmentDuration = "PseudoLive:SegmentDurationSeconds";
        private const string CfgWindowSize = "PseudoLive:WindowSize";

        // Status constants — single source of truth, avoids magic strings in branches
        private const string StatusLive     = "Live";
        private const string StatusRecorded = "Recorded";

        public GetPseudoLivePlaylistQueryHandler(
            ISessionValidationService sessionValidationService,
            IBlobStorageService blobStorageService,
            IPseudoLiveService pseudoLiveService,
            IConfiguration configuration,
            ILogger<GetPseudoLivePlaylistQueryHandler> logger,
            ISessionRepository sessionRepository)        // ── NEW dependency
        {
            _sessionValidationService = sessionValidationService;
            _blobStorageService = blobStorageService;
            _pseudoLiveService = pseudoLiveService;
            _configuration = configuration;
            _logger = logger;
            _sessionRepository = sessionRepository;     // ── NEW
        }

        public async Task<GetPseudoLivePlaylistResult> Handle(
            GetPseudoLivePlaylistQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[PseudoLive:Playlist] Request for sessionId={SessionId}, playlistName={PlaylistName} via MediatR",
                request.SessionId, request.PlaylistName ?? "(auto)");

            // ── STEP 1: Fetch the session to determine its Status ─────────────────────
            //
            // We read the session directly first so we know whether it is Live or Recorded
            // before deciding which validation path to follow.
            //
            // • Live     → ValidateSessionActiveAsync enforces the active-time window (unchanged).
            // • Recorded → Skip the active-time check; a recorded session can be watched at any
            //              time, even after session.EndTime has passed.

            var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning(
                    "[PseudoLive:Playlist] Session {SessionId} not found.", request.SessionId);
                throw new Domain.Exceptions.NotFoundException("Session not found.");
            }

            _logger.LogInformation(
                "[PseudoLive:Playlist] Session {SessionId} has Status={Status}",
                request.SessionId, session.Status);

            if (session.Status == StatusLive)
            {
                // ════════════════════════════════════════════════════════════════════
                //  LIVE PATH — existing implementation, completely unchanged
                // ════════════════════════════════════════════════════════════════════

                // 1. Validate the session is active (throws domain exceptions on failure)
                var liveSession = await _sessionValidationService.ValidateSessionActiveAsync(
                    request.SessionId, "Playlist", cancellationToken);

                // 2. List all .m3u8 files under session-live-video/{sessionId}/
                var prefix = $"{LiveVideoFolder}/{request.SessionId}/";
                var allBlobs = await _blobStorageService.ListBlobsAsync(prefix, cancellationToken);

                var playlists = allBlobs
                    .Where(b => b.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                    .Select(b => Path.GetFileName(b))
                    .OrderBy(n => n)
                    .ToList();

                _logger.LogInformation(
                    "[PseudoLive:Playlist] Found {Count} playlist(s) for sessionId={SessionId}: {Names}",
                    playlists.Count, request.SessionId, string.Join(", ", playlists));

                if (playlists.Count == 0)
                {
                    throw new Domain.Exceptions.NotFoundException(
                        $"No HLS playlists found for session '{request.SessionId}'.");
                }

                // 3. Resolve which playlist file to serve
                string resolvedPlaylistName;

                if (!string.IsNullOrWhiteSpace(request.PlaylistName))
                {
                    // Explicit request — validate it exists
                    if (!playlists.Contains(request.PlaylistName, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Domain.Exceptions.NotFoundException(
                            $"Playlist '{request.PlaylistName}' was not found for session '{request.SessionId}'.");
                    }
                    resolvedPlaylistName = request.PlaylistName;
                }
                else if (playlists.Count == 1)
                {
                    // Only one playlist — auto-select it for backward compatibility
                    resolvedPlaylistName = playlists[0];
                    _logger.LogInformation(
                        "[PseudoLive:Playlist] Single playlist auto-selected: {Name}", resolvedPlaylistName);
                }
                else
                {
                    // Multiple playlists, no selection made — return the list so the client can choose
                    _logger.LogInformation(
                        "[PseudoLive:Playlist] Multiple playlists found, returning list to client.");
                    return new GetPseudoLivePlaylistResult { AvailablePlaylists = playlists };
                }

                // 4. Fetch the raw .m3u8 content from Azure Blob Storage
                var blobPath = $"{prefix}{resolvedPlaylistName}";
                var blobStream = await _blobStorageService.GetBlobStreamAsync(blobPath, cancellationToken);

                if (blobStream == null)
                {
                    throw new Domain.Exceptions.NotFoundException(
                        $"Playlist file '{resolvedPlaylistName}' could not be read from storage.");
                }

                string rawContent;
                using (var reader = new StreamReader(blobStream, Encoding.UTF8))
                {
                    rawContent = await reader.ReadToEndAsync(cancellationToken);
                }

                // 5. Parse the VOD playlist to determine total segment count and segment duration
                var totalSegments = _pseudoLiveService.ParseSegmentCount(rawContent);
                var maxAllowedIndex = Math.Max(0, totalSegments - 1);

                _logger.LogInformation(
                    "[PseudoLive:Playlist] Parsed VOD playlist: {TotalSegments} segments (indices 0-{MaxIndex})",
                    totalSegments, maxAllowedIndex);

                // 6. Calculate the current playback position based on server time
                var segmentDuration = _configuration.GetValue<int>(CfgSegmentDuration, 6);
                var windowSize = _configuration.GetValue<int>(CfgWindowSize, 6);

                var currentSegmentIndex = _pseudoLiveService.GetCurrentSegmentIndex(
                    liveSession.StartTime, segmentDuration);

                _logger.LogInformation(
                    "[PseudoLive:Playlist] Current segment index: {Current} (max available: {Max})",
                    currentSegmentIndex, maxAllowedIndex);

                // 7. Generate the dynamic pseudo-live playlist with sliding window
                var baseSegmentUrl = $"{request.BasePath}/segment";
                var playlistContent = _pseudoLiveService.GenerateLivePlaylist(
                    request.SessionId,
                    currentSegmentIndex,
                    windowSize,
                    segmentDuration,
                    baseSegmentUrl,
                    maxAllowedIndex);

                _logger.LogInformation(
                    "[PseudoLive:Playlist] Generated dynamic pseudo-live playlist for sessionId={SessionId}",
                    request.SessionId);

                return new GetPseudoLivePlaylistResult { PlaylistContent = playlistContent };
            }
            else if (session.Status == StatusRecorded)
            {
                // ════════════════════════════════════════════════════════════════════
                //  RECORDED PATH — new VOD implementation
                //
                //  Goals:
                //  • Return the complete playlist (all segments, no sliding window).
                //  • Include #EXT-X-PLAYLIST-TYPE:VOD and #EXT-X-ENDLIST so the HLS
                //    player exposes a full seek bar and allows random access.
                //  • Do NOT calculate current live time or trim old segments.
                // ════════════════════════════════════════════════════════════════════

                // Fetch blob listing to find the .m3u8 file (same container layout as Live)
                var vodPrefix = $"{LiveVideoFolder}/{request.SessionId}/";
                var vodAllBlobs = await _blobStorageService.ListBlobsAsync(vodPrefix, cancellationToken);

                var vodPlaylists = vodAllBlobs
                    .Where(b => b.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                    .Select(b => Path.GetFileName(b))
                    .OrderBy(n => n)
                    .ToList();

                _logger.LogInformation(
                    "[PseudoLive:Playlist:VOD] Found {Count} playlist(s) for sessionId={SessionId}: {Names}",
                    vodPlaylists.Count, request.SessionId, string.Join(", ", vodPlaylists));

                if (vodPlaylists.Count == 0)
                {
                    throw new Domain.Exceptions.NotFoundException(
                        $"No HLS playlists found for Recorded session '{request.SessionId}'.");
                }

                // Resolve which playlist file to serve (same rules as Live)
                string vodResolvedName;

                if (!string.IsNullOrWhiteSpace(request.PlaylistName))
                {
                    if (!vodPlaylists.Contains(request.PlaylistName, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Domain.Exceptions.NotFoundException(
                            $"Playlist '{request.PlaylistName}' was not found for session '{request.SessionId}'.");
                    }
                    vodResolvedName = request.PlaylistName;
                }
                else if (vodPlaylists.Count == 1)
                {
                    vodResolvedName = vodPlaylists[0];
                    _logger.LogInformation(
                        "[PseudoLive:Playlist:VOD] Single playlist auto-selected: {Name}", vodResolvedName);
                }
                else
                {
                    _logger.LogInformation(
                        "[PseudoLive:Playlist:VOD] Multiple playlists found, returning list to client.");
                    return new GetPseudoLivePlaylistResult { AvailablePlaylists = vodPlaylists };
                }

                // Fetch the raw .m3u8 from blob storage to count total segments
                var vodBlobPath = $"{vodPrefix}{vodResolvedName}";
                var vodBlobStream = await _blobStorageService.GetBlobStreamAsync(vodBlobPath, cancellationToken);

                if (vodBlobStream == null)
                {
                    throw new Domain.Exceptions.NotFoundException(
                        $"Playlist file '{vodResolvedName}' could not be read from storage.");
                }

                string vodRawContent;
                using (var reader = new StreamReader(vodBlobStream, Encoding.UTF8))
                {
                    vodRawContent = await reader.ReadToEndAsync(cancellationToken);
                }

                // Count total segments from the stored playlist
                var vodTotalSegments = _pseudoLiveService.ParseSegmentCount(vodRawContent);

                if (vodTotalSegments <= 0)
                {
                    _logger.LogWarning(
                        "[PseudoLive:Playlist:VOD] ParseSegmentCount returned {Count} for sessionId={SessionId}. " +
                        "Falling back to TotalHlsSegments from DB.",
                        vodTotalSegments, request.SessionId);

                    // Fallback: use the pre-stored segment count from the session record
                    vodTotalSegments = session.TotalHlsSegments ?? 0;
                }

                if (vodTotalSegments <= 0)
                {
                    throw new Domain.Exceptions.NotFoundException(
                        $"Could not determine segment count for Recorded session '{request.SessionId}'.");
                }

                _logger.LogInformation(
                    "[PseudoLive:Playlist:VOD] Total segments for sessionId={SessionId}: {Total}",
                    request.SessionId, vodTotalSegments);

                // Read segment duration from configuration (same key as Live)
                var vodSegmentDuration = _configuration.GetValue<int>(CfgSegmentDuration, 6);
                var vodBaseSegmentUrl  = $"{request.BasePath}/segment";

                // Generate the complete VOD playlist — no sliding window, includes #EXT-X-ENDLIST
                var vodPlaylistContent = _pseudoLiveService.GenerateVodPlaylist(
                    request.SessionId,
                    vodTotalSegments,
                    vodSegmentDuration,
                    vodBaseSegmentUrl);

                _logger.LogInformation(
                    "[PseudoLive:Playlist:VOD] Generated complete VOD playlist for sessionId={SessionId} " +
                    "with {Total} segment(s).",
                    request.SessionId, vodTotalSegments);

                return new GetPseudoLivePlaylistResult { PlaylistContent = vodPlaylistContent };
            }
            else
            {
                // Unknown status — treat as a configuration error
                _logger.LogWarning(
                    "[PseudoLive:Playlist] Unknown session Status='{Status}' for sessionId={SessionId}.",
                    session.Status, request.SessionId);
                throw new Domain.Exceptions.NotFoundException(
                    $"Session '{request.SessionId}' has an unsupported status '{session.Status}'.");
            }
        }
    }
}
