//using Domain.Dto;
//using Domain.Enums;
//using Domain.Interfaces;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.StopLiveStream
//{
//    /// <summary>
//    /// Handles <see cref="StopLiveStreamCommand"/>.
//    ///
//    /// Business rules enforced:
//    ///   1. A live stream for the given session must exist.
//    ///   2. The live stream must currently be in <see cref="LiveStreamStatus.Live"/> state.
//    ///   3. The requesting speaker must be the owner of that stream.
//    ///   4. Status is transitioned to <see cref="LiveStreamStatus.Ended"/> and EndedAt is stamped.
//    ///   5. The parent Session.Status is also updated to "ended".
//    /// </summary>
//    public class StopLiveStreamCommandHandler : IRequestHandler<StopLiveStreamCommand, StopLiveStreamResponseDto>
//    {
//        private readonly ILiveStreamRepository _repo;

//        public StopLiveStreamCommandHandler(ILiveStreamRepository repo)
//        {
//            _repo = repo;
//        }

//        public async Task<StopLiveStreamResponseDto> Handle(
//            StopLiveStreamCommand request,
//            CancellationToken cancellationToken)
//        {
//            // ── 1. Session must exist ─────────────────────────────────────
//            if (!await _repo.SessionExistsAsync(request.SessionId, cancellationToken))
//                throw new KeyNotFoundException($"Session '{request.SessionId}' was not found.");

//            // ── 2. There must be an active live stream for this session ───
//            if (!await _repo.IsSessionAlreadyLiveAsync(request.SessionId, cancellationToken))
//                throw new InvalidOperationException(
//                    $"Session '{request.SessionId}' does not have an active live stream to stop.");

//            // ── 3. Stop and persist ───────────────────────────────────────
//            var endedAt = DateTime.UtcNow;
//            var result  = await _repo.StopLiveStreamAsync(
//                request.SessionId,
//                request.SpeakerId,
//                endedAt,
//                cancellationToken);

//            return new StopLiveStreamResponseDto
//            {
//                Success      = true,
//                Message      = "Live stream stopped successfully.",
//                SessionId    = request.SessionId,
//                LiveStreamId = result.LiveStreamId,
//                EndedAt      = endedAt,
//                Status       = LiveStreamStatus.Ended.ToString()
//            };
//        }
//    }
//}
