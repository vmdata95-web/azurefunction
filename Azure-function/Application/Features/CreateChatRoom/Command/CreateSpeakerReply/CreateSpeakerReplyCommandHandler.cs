using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CreateChatRoom.Command.CreateSpeakerReply
{
    /// <summary>
    /// Handles speaker replies with full business-rule enforcement.
    ///
    /// Business rules applied:
    ///   1. ReplyToMessageId is mandatory — a reply must reference an existing message.
    ///   2. The original message must exist and must not be soft-deleted.
    ///   3. The calling user must have a Speaker record (verified via ISpeakerRepository).
    ///   4. MessageType must be "public" or "private" (also enforced by the validator).
    ///   5. private → ReceiverUserId is auto-set to the original question owner's UserId.
    ///              Frontend-supplied ReceiverUserId is silently overridden.
    ///   6. public  → ReceiverUserId is always null.
    ///   7. SignalR routing:
    ///        public  → broadcast to room group  (SendMessageToRoomAsync)
    ///        private → targeted send to speaker + question owner only (SendPrivateMessageAsync × 2)
    ///              Never written to the room group — zero leakage guaranteed.
    /// </summary>
    public class CreateSpeakerReplyCommandHandler
        : IRequestHandler<CreateSpeakerReplyCommand, Guid>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISpeakerRepository _speakerRepository;
        private readonly IChatHubService _chatHubService;
        private readonly ILogger<CreateSpeakerReplyCommandHandler> _logger;

        public CreateSpeakerReplyCommandHandler(
            IChatRoomRepository chatRoomRepository,
            IUserRepository userRepository,
            ISpeakerRepository speakerRepository,
            IChatHubService chatHubService,
            ILogger<CreateSpeakerReplyCommandHandler> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _userRepository     = userRepository;
            _speakerRepository  = speakerRepository;
            _chatHubService     = chatHubService;
            _logger             = logger;
        }

        public async Task<Guid> Handle(
            CreateSpeakerReplyCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SpeakerReply] Request received. UserId={UserId} RoomId={RoomId} " +
                "MessageType={MessageType} ReplyToMessageId={ReplyToMessageId}",
                request.UserId, request.ChatRoomId,
                request.MessageType, request.ReplyToMessageId);

            // ── 1. ReplyToMessageId guard ─────────────────────────────────────
            // The FluentValidation validator also checks this, but we guard here
            // as defence-in-depth in case the handler is called directly in tests.
            if (request.ReplyToMessageId == null)
            {
                _logger.LogWarning(
                    "[SpeakerReply] Rejected — ReplyToMessageId is null. UserId={UserId}",
                    request.UserId);
                throw new Exception("ReplyToMessageId is required.");
            }

            // ── 2. Load original message ──────────────────────────────────────
            var originalMessage = await _chatRoomRepository
                .GetMessageByIdAsync(request.ReplyToMessageId.Value, cancellationToken);

            if (originalMessage is null)
            {
                _logger.LogWarning(
                    "[SpeakerReply] Original message not found or deleted. " +
                    "ReplyToMessageId={ReplyToMessageId}",
                    request.ReplyToMessageId.Value);
                throw new Exception("Original message not found.");
            }

            _logger.LogInformation(
                "[SpeakerReply] Original message found. OriginalMessageId={OriginalMessageId} " +
                "OriginalUserId={OriginalUserId}",
                originalMessage.Id, originalMessage.UserId);

            // ── 3. Verify chat room exists ────────────────────────────────────
            var roomExists = await _chatRoomRepository
                .ChatRoomExistsAsync(request.ChatRoomId);

            if (!roomExists)
            {
                _logger.LogWarning(
                    "[SpeakerReply] Chat room not found. ChatRoomId={ChatRoomId}",
                    request.ChatRoomId);
                throw new Exception("Chat room not found.");
            }

            // ── 4. Verify caller is a registered Speaker ──────────────────────
            // We check the Speaker table (not just the User.Role string) so that
            // the validation is always consistent with how speakers are created
            // in CreateSpeakerCommandHandler.
            var isSpeaker = await _speakerRepository
                .SpeakerExistsAsync(request.UserId);

            if (!isSpeaker)
            {
                _logger.LogWarning(
                    "[SpeakerReply] Forbidden — caller is not a registered speaker. " +
                    "UserId={UserId}",
                    request.UserId);
                throw new Exception("Only a registered speaker can post a speaker reply.");
            }

            _logger.LogInformation(
                "[SpeakerReply] Speaker validation passed. UserId={UserId}", request.UserId);

            // ── 5. Load speaker's display name (for the SignalR payload) ──────
            var speakerUser = await _userRepository.GetByIdAsync(request.UserId);
            if (speakerUser is null)
            {
                _logger.LogWarning(
                    "[SpeakerReply] Speaker user record not found. UserId={UserId}",
                    request.UserId);
                throw new Exception("Speaker user record not found.");
            }

            // ── 6. Enforce MessageType ────────────────────────────────────────
            if (request.MessageType != "public" && request.MessageType != "private")
            {
                _logger.LogWarning(
                    "[SpeakerReply] Invalid MessageType='{MessageType}'. UserId={UserId}",
                    request.MessageType, request.UserId);
                throw new Exception("MessageType must be 'public' or 'private'.");
            }

            // ── 7. Enforce ReceiverUserId server-side ─────────────────────────
            // Overrides any client-supplied value unconditionally.
            Guid? receiverUserId = request.MessageType == "private"
                ? originalMessage.UserId    // always the question owner
                : (Guid?)null;              // public replies have no receiver

            _logger.LogInformation(
                "[SpeakerReply] ReceiverUserId resolved to {ReceiverUserId} for MessageType={MessageType}",
                receiverUserId, request.MessageType);

            // ── 8. Persist message ────────────────────────────────────────────
            var message = new Message
            {
                Id              = Guid.NewGuid(),
                ChatRoomId      = request.ChatRoomId,
                UserId          = request.UserId,
                MessageText     = request.Message,
                MessageType     = request.MessageType,
                ReceiverUserId  = receiverUserId,
                ReplyToMessageId = request.ReplyToMessageId,
                SenderType      = "SPEAKER",
                CreatedAt       = DateTime.UtcNow,
                IsDeleted       = false
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
                "[SpeakerReply] Message persisted. MessageId={MessageId} " +
                "RoomId={RoomId} MessageType={MessageType} ReceiverUserId={ReceiverUserId}",
                message.Id, request.ChatRoomId, request.MessageType, receiverUserId);

            // ── 9. SignalR dispatch ───────────────────────────────────────────
            // Build a unified payload shape that mirrors GetMessagesResponse
            // so the frontend can handle real-time and historical messages identically.
            var payload = new
            {
                Id               = message.Id,
                ChatRoomId       = message.ChatRoomId,
                UserId           = message.UserId,
                Name             = speakerUser.Name,
                Message          = message.MessageText,
                MessageType      = message.MessageType,
                ReceiverUserId   = message.ReceiverUserId,
                ReplyToMessageId = message.ReplyToMessageId,
                SenderType       = message.SenderType,
                CreatedAt        = message.CreatedAt
            };

            if (request.MessageType == "public")
            {
                // ── PUBLIC: broadcast to entire room ──────────────────────────
                _logger.LogInformation(
                    "[SpeakerReply][SignalR] Dispatching public reply to room={RoomId}",
                    request.ChatRoomId);

                await _chatHubService.SendMessageToRoomAsync(request.ChatRoomId, payload);
            }
            else
            {
                // ── PRIVATE: targeted delivery — NO room broadcast ─────────────
                // Send to original question owner
                _logger.LogInformation(
                    "[SpeakerReply][SignalR] Dispatching private reply to " +
                    "QuestionOwner={QuestionOwnerUserId} and Speaker={SpeakerUserId}",
                    originalMessage.UserId, request.UserId);

                await _chatHubService.SendPrivateMessageAsync(
                    originalMessage.UserId, payload);

                // Also send to the speaker themselves so their own UI reflects the sent reply
                if (request.UserId != originalMessage.UserId)
                {
                    await _chatHubService.SendPrivateMessageAsync(
                        request.UserId, payload);
                }
            }

            _logger.LogInformation(
                "[SpeakerReply] Completed successfully. MessageId={MessageId}",
                message.Id);

            return message.Id;
        }
    }
}