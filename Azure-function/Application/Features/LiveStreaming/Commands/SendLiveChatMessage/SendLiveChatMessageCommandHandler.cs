using Domain.Dto;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.LiveStreaming.Commands.SendLiveChatMessage
{
    /// <summary>
    /// Handles <see cref="SendLiveChatMessageCommand"/>.
    ///
    /// Business rules enforced:
    ///   1. The session must exist.
    ///   2. The session must currently be live.
    ///   3. The user must exist.
    ///   4. The message is persisted using the existing Message/ChatRoom structure
    ///      via ILiveStreamRepository (which creates the Message record under the
    ///      session's associated ChatRoom, or a synthetic one if none exists).
    ///   5. The message is broadcast to SignalR clients joined to that room group
    ///      via IChatHubService, matching the existing SendMessage handler pattern.
    /// </summary>
    public class SendLiveChatMessageCommandHandler
        : IRequestHandler<SendLiveChatMessageCommand, SendLiveChatMessageResponseDto>
    {
        private readonly ILiveStreamRepository _repo;
        private readonly IChatHubService       _hub;

        public SendLiveChatMessageCommandHandler(
            ILiveStreamRepository repo,
            IChatHubService hub)
        {
            _repo = repo;
            _hub  = hub;
        }

        public async Task<SendLiveChatMessageResponseDto> Handle(
            SendLiveChatMessageCommand request,
            CancellationToken cancellationToken)
        {
            // ── 1. Session must exist ─────────────────────────────────────
            if (!await _repo.SessionExistsAsync(request.SessionId, cancellationToken))
                throw new KeyNotFoundException($"Session '{request.SessionId}' was not found.");

            // ── 2. Session must be live ───────────────────────────────────
            if (!await _repo.IsSessionAlreadyLiveAsync(request.SessionId, cancellationToken))
                throw new InvalidOperationException(
                    $"Session '{request.SessionId}' does not have an active live stream.");

            // ── 3. User must exist ────────────────────────────────────────
            if (!await _repo.UserExistsAsync(request.UserId, cancellationToken))
                throw new KeyNotFoundException($"User '{request.UserId}' was not found.");

            // ── 4. Persist the message ────────────────────────────────────
            var sentAt = DateTime.UtcNow;
            var messageId = await _repo.SaveLiveChatMessageAsync(
                request.SessionId,
                request.UserId,
                request.MessageText,
                sentAt,
                cancellationToken);

            // ── 5. Broadcast via SignalR (fire-and-forget style) ──────────
            //      The RoomId for a live stream maps to the SignalR group key.
            //      ChatHubService swallows errors so broadcast failures never
            //      cause an API failure. Message is already persisted above.
            var streamInfo = await _repo.GetLiveStreamBySessionIdAsync(request.SessionId, cancellationToken);
            if (streamInfo is not null)
            {
                var broadcastPayload = new
                {
                    messageId   = messageId,
                    sessionId   = request.SessionId,
                    userId      = request.UserId,
                    messageText = request.MessageText,
                    sentAt      = sentAt
                };

                await _hub.SendMessageToRoomAsync(streamInfo.RoomId, broadcastPayload);
            }

            return new SendLiveChatMessageResponseDto
            {
                Success     = true,
                Message     = "Message sent successfully.",
                MessageId   = messageId,
                SessionId   = request.SessionId,
                UserId      = request.UserId,
                MessageText = request.MessageText,
                SentAt      = sentAt
            };
        }
    }
}
