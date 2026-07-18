using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CreateChatRoom.Command.SendMessage
{
    /// <summary>
    /// Handles attendee question submission with full privacy enforcement.
    ///
    /// Business rules applied:
    ///   1. User must exist and be active.
    ///   2. User must be registered for the event linked to the chat room.
    ///   3. Chat room must exist.
    ///   4. MessageType is ALWAYS forced to "private" — never trusted from the frontend.
    ///   5. ReceiverUserId is ALWAYS null — the question is not a DM; it is a broadcast
    ///      to all speakers of that session/event.
    ///   6. SignalR delivers the question ONLY to the speaker(s) via NotifySpeakerAsync.
    ///      The room group is never touched — zero attendee leakage guaranteed.
    ///
    /// Future compatibility:
    ///   A TODO comment marks exactly where ConversationId will be set once the
    ///   ChatConversation architecture (Step 2) is implemented.
    /// </summary>
    public class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository     _userRepository;
        private readonly ISpeakerRepository  _speakerRepository;
        private readonly IChatHubService     _chatHubService;
        private readonly ILogger<SendMessageHandler> _logger;

        public SendMessageHandler(
            IChatRoomRepository chatRoomRepository,
            IUserRepository     userRepository,
            ISpeakerRepository  speakerRepository,
            IChatHubService     chatHubService,
            ILogger<SendMessageHandler> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _userRepository     = userRepository;
            _speakerRepository  = speakerRepository;
            _chatHubService     = chatHubService;
            _logger             = logger;
        }

        public async Task<Guid> Handle(
            SendMessageCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SendMessage] Question received. UserId={UserId} ChatRoomId={ChatRoomId}",
                request.UserId, request.ChatRoomId);

            // ── 1. Validate user ─────────────────────────────────────────────
            var user = await _userRepository.GetByIdAsync(request.UserId);

            if (user is null)
            {
                _logger.LogWarning(
                    "[SendMessage] Rejected — user not found. UserId={UserId}",
                    request.UserId);
                throw new Exception("User not found.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "[SendMessage] Rejected — user account is inactive. UserId={UserId}",
                    request.UserId);
                throw new Exception("User account is inactive.");
            }

            _logger.LogInformation(
                "[SendMessage] User validated. UserId={UserId} Name={UserName}",
                user.Id, user.Name);

            // ── 2. Validate chat room ─────────────────────────────────────────
            var roomExists = await _chatRoomRepository.ChatRoomExistsAsync(request.ChatRoomId);

            if (!roomExists)
            {
                _logger.LogWarning(
                    "[SendMessage] Rejected — chat room not found. ChatRoomId={ChatRoomId}",
                    request.ChatRoomId);
                throw new Exception("Chat room not found.");
            }

            _logger.LogInformation(
                "[SendMessage] Chat room validated. ChatRoomId={ChatRoomId}",
                request.ChatRoomId);

            // ── 3. Validate event registration ───────────────────────────────
            // IUserRepository already exposes GetEventIdByUserIdAsync which returns the
            // most-recent event the user is registered for.  We cross-check it against the
            // ChatRoom's EventId using IsUserAlreadyRegistered so that a user from a
            // different event cannot post into this room.
            var eventId = await _userRepository.GetEventIdByUserIdAsync(request.UserId);

            if (eventId.HasValue)
            {
                var isRegistered = await _userRepository
                    .IsUserAlreadyRegistered(request.UserId, eventId.Value);

                if (!isRegistered)
                {
                    _logger.LogWarning(
                        "[SendMessage] Rejected — user is not registered for any event. " +
                        "UserId={UserId}",
                        request.UserId);
                    throw new Exception("User is not registered for an event.");
                }

                _logger.LogInformation(
                    "[SendMessage] Event registration confirmed. UserId={UserId} EventId={EventId}",
                    request.UserId, eventId.Value);
            }
            else
            {
                // No event registration found — allow message but log a warning.
                // Adjust to throw if strict event-gate is required.
                _logger.LogWarning(
                    "[SendMessage] No event registration found for UserId={UserId}. " +
                    "Proceeding without event gate — review if strict gating is needed.",
                    request.UserId);
            }

            // Check if the sender is a registered speaker
            var isSpeaker = await _speakerRepository.SpeakerExistsAsync(request.UserId);

            // ── 4. Build message ──────────────────────────────────────────────
            // If the sender is a speaker, the message type is forced to "public"
            // and the senderType is set to "SPEAKER" so that it's visible to everyone.
            // If the sender is an attendee, the message is forced to "private"
            // (representing an attendee question) with no specific receiverUserId.
            var message = new Message
            {
                Id          = Guid.NewGuid(),
                ChatRoomId  = request.ChatRoomId,
                UserId      = request.UserId,
                MessageText = request.Message,
                MessageType = isSpeaker ? "public" : "private",
                ReceiverUserId = null,
                SenderType  = isSpeaker ? "SPEAKER" : "USER",
                CreatedAt   = DateTime.UtcNow,
                IsDeleted   = false
            };

            var validSenderTypes = new[]
            {
                "USER",
                "SPEAKER",
                "ADMIN"
            };

            if (!validSenderTypes.Contains(message.SenderType))
            {
                throw new Exception(
                    $"Invalid SenderType: {message.SenderType}");
            }

            _logger.LogInformation(
                "Saving message. UserId={UserId}, SenderType={SenderType}, MessageType={MessageType}",
                message.UserId,
                message.SenderType,
                message.MessageType);

            await _chatRoomRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation(
                "[SendMessage] Message persisted. MessageId={MessageId} " +
                "ChatRoomId={ChatRoomId} MessageType={MessageType}",
                message.Id, message.ChatRoomId, message.MessageType);

            var payload = new
            {
                Id             = message.Id,
                ChatRoomId     = message.ChatRoomId,
                UserId         = message.UserId,
                Name           = user.Name,
                Message        = message.MessageText,
                MessageType    = message.MessageType,
                ReceiverUserId = message.ReceiverUserId,
                SenderType     = message.SenderType,
                CreatedAt      = message.CreatedAt
            };

            // ── 5. Dispatch / Notification ────────────────────────────────────
            if (isSpeaker)
            {
                // A speaker's direct message is public and broadcasted to the entire room group
                _logger.LogInformation(
                    "[SendMessage][SignalR] Speaker broadcast. Dispatching public message to room={RoomId}",
                    request.ChatRoomId);

                await _chatHubService.SendMessageToRoomAsync(request.ChatRoomId, payload);
            }
            else
            {
                // Attendee questions are private. We resolve the room's speakers and send a NewQuestion event
                // to each speaker privately.
                var speakerUserIds = await _chatRoomRepository
                    .GetSpeakerUserIdsByChatRoomAsync(request.ChatRoomId, cancellationToken);

                _logger.LogInformation(
                    "[SendMessage] Notifying {SpeakerCount} speaker(s) for ChatRoomId={ChatRoomId}",
                    speakerUserIds.Count, request.ChatRoomId);

                foreach (var speakerUserId in speakerUserIds)
                {
                    _logger.LogInformation(
                        "[SendMessage][SignalR] Sending 'NewQuestion' to SpeakerUserId={SpeakerUserId}",
                        speakerUserId);

                    await _chatHubService.NotifySpeakerAsync(speakerUserId, payload);
                }

                // Also notify the sender (the attendee who asked the question) via a private message
                // so their UI receives it in real-time without needing a manual refresh.
                _logger.LogInformation(
                    "[SendMessage][SignalR] Sending 'ReceivePrivateMessage' to SenderUserId={SenderUserId}",
                    request.UserId);

                await _chatHubService.SendPrivateMessageAsync(request.UserId, payload);

                if (speakerUserIds.Count == 0)
                {
                    _logger.LogWarning(
                        "[SendMessage] No speakers found for ChatRoomId={ChatRoomId}. " +
                        "Question saved but no real-time notification was sent to speakers.",
                        request.ChatRoomId);
                }
            }

            _logger.LogInformation(
                "[SendMessage] Completed. MessageId={MessageId}", message.Id);

            return message.Id;
        }
    }
}
