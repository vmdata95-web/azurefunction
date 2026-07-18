//using Domain.Dto;
//using Domain.Entities;
//using Domain.Enums;
//using Domain.Interfaces;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.LiveStreaming
//{
//    /// <summary>
//    /// Handles <see cref="StartLiveStreamCommand"/>.
//    ///
//    /// Business rules enforced:
//    ///   1. The referenced session must exist.
//    ///   2. The referenced room must exist.
//    ///   3. The referenced speaker must exist.
//    ///   4. The session must not already be live (no duplicate active streams).
//    ///   5. A new <see cref="LiveStream"/> record is created with status=Live.
//    ///   6. The live stream's metadata is returned to the caller.
//    ///
//    /// SignalR hook (future):
//    ///   Inject <see cref="IChatHubService"/> here and call
//    ///   SendMessageToRoomAsync(roomId, payload) to broadcast "StreamStarted"
//    ///   to connected viewers when the stream goes live.
//    /// </summary>
//    public class StartLiveStreamCommandHandler : IRequestHandler<StartLiveStreamCommand, StartLiveStreamResponseDto>
//    {
//        private readonly ILiveStreamRepository _liveStreamRepository;

//        public StartLiveStreamCommandHandler(ILiveStreamRepository liveStreamRepository)
//        {
//            _liveStreamRepository = liveStreamRepository;
//        }

//        public async Task<StartLiveStreamResponseDto> Handle(
//            StartLiveStreamCommand request,
//            CancellationToken cancellationToken)
//        {
//            // ── 1. Validate session exists ────────────────────────────────
//            if (!await _liveStreamRepository.SessionExistsAsync(request.SessionId, cancellationToken))
//                throw new KeyNotFoundException($"Session '{request.SessionId}' was not found.");

//            // ── 2. Validate room exists ───────────────────────────────────
//            if (!await _liveStreamRepository.RoomExistsAsync(request.RoomId, cancellationToken))
//                throw new KeyNotFoundException($"Room '{request.RoomId}' was not found.");

//            // ── 3. Validate speaker exists ────────────────────────────────
//            if (!await _liveStreamRepository.SpeakerExistsAsync(request.SpeakerId, cancellationToken))
//                throw new KeyNotFoundException($"Speaker '{request.SpeakerId}' was not found.");

//            // ── 4. Guard: session must not already be live ────────────────
//            if (await _liveStreamRepository.IsSessionAlreadyLiveAsync(request.SessionId, cancellationToken))
//                throw new InvalidOperationException(
//                    $"Session '{request.SessionId}' already has an active live stream.");

//            // ── 5. Build the live stream record ───────────────────────────
//            var liveStream = new LiveStream
//            {
//                Id        = Guid.NewGuid(),
//                SessionId = request.SessionId,
//                RoomId    = request.RoomId,
//                SpeakerId = request.SpeakerId,
//                Status    = LiveStreamStatus.Live,
//                StartedAt = DateTime.UtcNow,
//                StreamKey = GenerateStreamKey(request.SessionId, request.SpeakerId),
//                Metadata  = "{}",
//                CreatedAt = DateTime.UtcNow
//            };

//            // ── 6. Persist ────────────────────────────────────────────────
//            var newId = await _liveStreamRepository.StartLiveStreamAsync(liveStream, cancellationToken);

//            // ── 7. Fetch back the stored projection for the response ──────
//            var dto = await _liveStreamRepository.GetLiveStreamBySessionIdAsync(request.SessionId, cancellationToken);

//            return new StartLiveStreamResponseDto
//            {
//                Success = true,
//                Message = "Live stream started successfully.",
//                Data    = dto
//            };
//        }

//        // ── Helpers ───────────────────────────────────────────────────────

//        /// <summary>
//        /// Creates a deterministic-but-opaque stream key from the session and speaker ids.
//        /// In production this would be a signed token from your streaming infrastructure.
//        /// </summary>
//        private static string GenerateStreamKey(Guid sessionId, Guid speakerId)
//        {
//            // Lightweight approach: combine both GUIDs and base64-encode.
//            // Replace with a proper signed token / CDN key when integrating streaming infra.
//            var raw = $"{sessionId:N}:{speakerId:N}:{DateTime.UtcNow.Ticks}";
//            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
//        }
//    }
//}
