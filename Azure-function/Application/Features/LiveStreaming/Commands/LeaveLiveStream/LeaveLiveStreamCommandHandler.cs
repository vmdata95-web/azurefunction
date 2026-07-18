//using Domain.Dto;
//using Domain.Enums;
//using Domain.Interfaces;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.LeaveLiveStream
//{
//    /// <summary>
//    /// Handles <see cref="LeaveLiveStreamCommand"/>.
//    ///
//    /// Business rules enforced:
//    ///   1. The session must exist.
//    ///   2. The user must exist.
//    ///   3. Leave activity is recorded in UserActivityLogs (Action = Out).
//    ///
//    /// Note: We allow leaving even if the stream has already ended
//    ///       (graceful disconnect scenario), so we do NOT reject on stream status.
//    /// </summary>
//    public class LeaveLiveStreamCommandHandler : IRequestHandler<LeaveLiveStreamCommand, LeaveLiveStreamResponseDto>
//    {
//        private readonly ILiveStreamRepository _repo;

//        public LeaveLiveStreamCommandHandler(ILiveStreamRepository repo)
//        {
//            _repo = repo;
//        }

//        public async Task<LeaveLiveStreamResponseDto> Handle(
//            LeaveLiveStreamCommand request,
//            CancellationToken cancellationToken)
//        {
//            // ── 1. Session must exist ─────────────────────────────────────
//            if (!await _repo.SessionExistsAsync(request.SessionId, cancellationToken))
//                throw new KeyNotFoundException($"Session '{request.SessionId}' was not found.");

//            // ── 2. User must exist ────────────────────────────────────────
//            if (!await _repo.UserExistsAsync(request.UserId, cancellationToken))
//                throw new KeyNotFoundException($"User '{request.UserId}' was not found.");

//            // ── 3. Record leave activity ──────────────────────────────────
//            var leftAt = DateTime.UtcNow;
//            await _repo.RecordUserActivityAsync(
//                request.UserId,
//                request.SessionId,
//                UserActivityAction.Out,
//                leftAt,
//                cancellationToken);

//            return new LeaveLiveStreamResponseDto
//            {
//                Success   = true,
//                Message   = "Left live stream successfully.",
//                SessionId = request.SessionId,
//                UserId    = request.UserId,
//                LeftAt    = leftAt
//            };
//        }
//    }
//}
