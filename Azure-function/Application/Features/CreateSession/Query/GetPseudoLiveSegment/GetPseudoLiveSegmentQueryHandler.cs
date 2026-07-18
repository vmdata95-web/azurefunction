using Application.Common.Interfaces;
using Domain.Exceptions;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Query.GetPseudoLiveSegment
{
    public class GetPseudoLiveSegmentQueryHandler : IRequestHandler<GetPseudoLiveSegmentQuery, Stream>
    {
        private readonly ISessionValidationService _sessionValidationService;
        private readonly IPseudoLiveService _pseudoLiveService;
        private readonly ISessionRepository _sessionRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetPseudoLiveSegmentQueryHandler> _logger;

        private const string CfgSegmentDuration = "PseudoLive:SegmentDurationSeconds";
        private const string CfgWindowSize = "PseudoLive:WindowSize";

        public GetPseudoLiveSegmentQueryHandler(
            ISessionValidationService sessionValidationService,
            IPseudoLiveService pseudoLiveService,
            ISessionRepository sessionRepository,
            IConfiguration configuration,
            ILogger<GetPseudoLiveSegmentQueryHandler> logger)
        {
            _sessionValidationService = sessionValidationService;
            _pseudoLiveService = pseudoLiveService;
            _sessionRepository = sessionRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Stream> Handle(GetPseudoLiveSegmentQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[PseudoLive:Segment] Request for sessionId={SessionId}, segmentIndex={Index}, fileName={FileName} via MediatR",
                request.SessionId, request.SegmentIndex, request.FileName ?? "(none)");

            // 1. Reject obviously invalid indices immediately (fast path)
            if (request.SegmentIndex < 0)
            {
                _logger.LogWarning(
                    "[PseudoLive:Segment] Rejected negative segmentIndex={Index} for sessionId={SessionId}",
                    request.SegmentIndex, request.SessionId);
                throw new BadRequestException("Invalid segment index.");
            }

            // 2. Load the session record to read its Status BEFORE choosing the validation path.
            //
            // WHY: ValidateSessionActiveAsync enforces StartTime/EndTime and throws
            // ForbiddenException("Session is not active.") when now > session.EndTime.
            // That is correct for Live sessions, but a Recorded session must be watchable
            // at any time — even after EndTime has passed.
            //
            // Strategy:
            //   • Live     → call ValidateSessionActiveAsync (full time-gate, unchanged).
            //   • Recorded → call GetByIdAsync only (existence check, no time gate).

            var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning(
                    "[PseudoLive:Segment] Session {SessionId} not found.", request.SessionId);
                throw new Domain.Exceptions.NotFoundException("Session not found.");
            }

            _logger.LogInformation(
                "[PseudoLive:Segment] Session {SessionId} has Status={Status}",
                request.SessionId, session.Status);

            // ───────────────────────────────────────────────────────────────────────
            // Branch on Session.Status:
            // • "Live"     → existing sliding-window validation (unchanged).
            // • "Recorded" → skip window check; allow random access to any valid index.
            // ───────────────────────────────────────────────────────────────────────

            if (session.Status == "Live")
            {
                // ════════════════════════════════════════════════════════════════════
                //  LIVE PATH — existing implementation, completely unchanged
                // ════════════════════════════════════════════════════════════════════

                // 3. Enforce the active-time gate: throws ForbiddenException if the
                //    session has not started yet or has already ended.
                //    This call is intentionally made ONLY for Live sessions.
                await _sessionValidationService.ValidateSessionActiveAsync(
                    request.SessionId, "Segment", cancellationToken);

                // 4. Read configuration for the sliding window check
                var segmentDuration = _configuration.GetValue<int>(CfgSegmentDuration, 10);
                var windowSize = _configuration.GetValue<int>(CfgWindowSize, 6);

                // 5. Compute the current live segment index based on elapsed time
                var currentIndex = _pseudoLiveService.GetCurrentSegmentIndex(
                    session.StartTime, segmentDuration);

                // 6. Clamp currentIndex to the total available segments (if known)
                var clampedCurrentIndex = _pseudoLiveService.GetClampedCurrentIndex(
                    currentIndex, session.TotalHlsSegments);

                // 7. Validate the segment request using VOD fallback logic
                if (!_pseudoLiveService.IsSegmentAllowedWithVod(
                    request.SegmentIndex, clampedCurrentIndex, windowSize, session.TotalHlsSegments))
                {
                    _logger.LogWarning(
                        "[PseudoLive:Segment] FORBIDDEN. SessionId={SessionId}, " +
                        "RequestedIndex={Req}, CurrentIndex={Cur}, ClampedIndex={Clamped}, " +
                        "TotalSegments={Total}, Window={Win}. Possible replay-attack or seek-back attempt.",
                        request.SessionId, request.SegmentIndex, currentIndex, clampedCurrentIndex,
                        session.TotalHlsSegments, windowSize);

                    // Do NOT reveal the currentIndex, clampedIndex or total in the error — prevents calibration attacks.
                    throw new ForbiddenException("Access to this segment is not permitted at this time.");
                }
            }
            else if (session.Status == "Recorded")
            {
                // ════════════════════════════════════════════════════════════════════
                //  RECORDED PATH — new VOD segment validation
                //
                //  The sliding-window check is INTENTIONALLY skipped here.
                //  A Recorded session is fully available for random access:
                //  seeking forward, seeking backward, pausing and resuming,
                //  and jumping to any timestamp are all permitted.
                //
                //  The only check performed is an upper-bound guard: if the total
                //  segment count is known, we reject requests beyond the last segment.
                // ════════════════════════════════════════════════════════════════════

                _logger.LogInformation(
                    "[PseudoLive:Segment:VOD] Recorded session — skipping sliding-window check. " +
                    "SessionId={SessionId}, RequestedIndex={Index}",
                    request.SessionId, request.SegmentIndex);

                // Bounds check: reject indices beyond the last known segment
                if (session.TotalHlsSegments.HasValue)
                {
                    var maxIndex = session.TotalHlsSegments.Value - 1;
                    if (request.SegmentIndex > maxIndex)
                    {
                        _logger.LogWarning(
                            "[PseudoLive:Segment:VOD] Requested index {Req} exceeds max={Max} " +
                            "for sessionId={SessionId}.",
                            request.SegmentIndex, maxIndex, request.SessionId);
                        throw new NotFoundException(
                            $"Segment {request.SegmentIndex} is not available.");
                    }
                }
                // If TotalHlsSegments is null (legacy), allow the request and let blob storage
                // return 404 naturally if the file does not exist.
            }
            else
            {
                // Unknown status — safe default: deny
                _logger.LogWarning(
                    "[PseudoLive:Segment] Unknown session Status='{Status}' for sessionId={SessionId}.",
                    session.Status, request.SessionId);
                throw new ForbiddenException("Access to this segment is not permitted.");
            }

            // 7. Determine the blob file name to retrieve
            //    Use the FileName from the rewritten playlist URL when available;
            //    fall back to a conventional naming pattern if not provided.
            var fileName = !string.IsNullOrWhiteSpace(request.FileName)
                ? request.FileName
                : $"index{request.SegmentIndex}.ts";

            // 8. Fetch the segment stream from the repository (Azure Blob Storage)
            var stream = await _sessionRepository.GetPseudoLiveSegmentAsync(
                request.SessionId,
                fileName,
                cancellationToken);

            if (stream == null)
            {
                _logger.LogWarning(
                    "[PseudoLive:Segment] Blob not found for SessionId={SessionId}, FileName={FileName}",
                    request.SessionId, fileName);

                throw new NotFoundException($"Segment {request.SegmentIndex} is not available.");
            }

            _logger.LogInformation(
                "[PseudoLive:Segment] Streaming segment {Index} (file: {FileName}) for sessionId={SessionId}",
                request.SegmentIndex, fileName, request.SessionId);

            return stream;
        }
    }
}
