//using Domain.Dto;
//using Domain.Enums;
//using Domain.Interfaces;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.JoinLiveStream
//{
//    /// <summary>
//    /// Handles <see cref="JoinLiveStreamCommand"/>.
//    ///
//    /// Business rules enforced:
//    ///   1. The session must exist.
//    ///   2. The session must currently be live (status = Live).
//    ///   3. The user must exist.
//    ///   4. Join activity is recorded in UserActivityLogs (Action = Enter).
//    ///   5. Stream key and room/session details are returned for the viewer.
//    /// </summary>
//    public class JoinLiveStreamCommandHandler : IRequestHandler<JoinLiveStreamCommand, JoinLiveStreamResponseDto>
//    {
//        private readonly ILiveStreamRepository _repo;

//        public JoinLiveStreamCommandHandler(ILiveStreamRepository repo)
//        {
//            _repo = repo;
//        }

//        public async Task<JoinLiveStreamResponseDto> Handle(
//            JoinLiveStreamCommand request,
//            CancellationToken cancellationToken)
//        {
//            // ── 1. Session must exist ─────────────────────────────────────
//            if (!await _repo.SessionExistsAsync(request.SessionId, cancellationToken))
//                throw new KeyNotFoundException($"Session '{request.SessionId}' was not found.");

//            // ── 2. Session must be live ───────────────────────────────────
//            if (!await _repo.IsSessionAlreadyLiveAsync(request.SessionId, cancellationToken))
//                throw new InvalidOperationException(
//                    $"Session '{request.SessionId}' does not have an active live stream.");

//            // ── 3. User must exist ────────────────────────────────────────
//            if (!await _repo.UserExistsAsync(request.UserId, cancellationToken))
//                throw new KeyNotFoundException($"User '{request.UserId}' was not found.");

//            // ── 4. Record join activity ───────────────────────────────────
//            var joinedAt = DateTime.UtcNow;
//            await _repo.RecordUserActivityAsync(
//                request.UserId,
//                request.SessionId,
//                UserActivityAction.Enter,
//                joinedAt,
//                cancellationToken);

//            // ── 5. Fetch stream details for response ──────────────────────
//            var streamInfo = await _repo.GetLiveStreamBySessionIdAsync(request.SessionId, cancellationToken);

//            return new JoinLiveStreamResponseDto
//            {
//                Success      = true,
//                Message      = "Joined live stream successfully.",
//                SessionId    = request.SessionId,
//                UserId       = request.UserId,
//                StreamKey    = streamInfo?.StreamKey    ?? string.Empty,
//                RoomName     = streamInfo?.RoomName     ?? string.Empty,
//                SessionTitle = streamInfo?.SessionTitle ?? string.Empty,
//                JoinedAt     = joinedAt
//            };
//        }
//    }
//}
