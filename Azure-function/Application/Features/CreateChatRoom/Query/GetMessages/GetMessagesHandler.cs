using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CreateChatRoom.Query.GetMessages
{
    /// <summary>
    /// Handles GET /{chatRoomId}/messages with correct role-based visibility.
    ///
    /// Visibility matrix:
    /// ┌───────────────────┬────────────┬──────────────────────────────────────────┐
    /// │ MessageType       │ Caller     │ Visible?                                 │
    /// ├───────────────────┼────────────┼──────────────────────────────────────────┤
    /// │ public            │ anyone     │ ✅ always                                │
    /// │ private (question)│ sender     │ ✅ own question                          │
    /// │ private (question)│ speaker    │ ✅ IsSpeaker=true → sees all questions  │
    /// │ private (question)│ other user │ ❌ filtered out                         │
    /// │ private (reply)   │ receiver   │ ✅ ReceiverUserId match (legacy rows)   │
    /// │ private (reply)   │ other user │ ❌ filtered out                         │
    /// └───────────────────┴────────────┴──────────────────────────────────────────┘
    ///
    /// Future compatibility:
    ///   A TODO comment marks where ConversationId-based filtering will replace
    ///   the current MessageType filter once Step 2 (ChatConversation) is live.
    /// </summary>
    public class GetMessagesHandler
        : IRequestHandler<GetMessagesQuery, List<GetMessagesResponse>>
    {
        private readonly IChatRoomRepository _repository;
        private readonly ISpeakerRepository  _speakerRepository;
        private readonly ILogger<GetMessagesHandler> _logger;

        public GetMessagesHandler(
            IChatRoomRepository repository,
            ISpeakerRepository  speakerRepository,
            ILogger<GetMessagesHandler> logger)
        {
            _repository        = repository;
            _speakerRepository = speakerRepository;
            _logger            = logger;
        }

        public async Task<List<GetMessagesResponse>> Handle(
            GetMessagesQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[GetMessages] Request. ChatRoomId={ChatRoomId} CurrentUserId={CurrentUserId} " +
                "Page={Page} PageSize={PageSize}",
                request.ChatRoomId, request.CurrentUserId, request.Page, request.PageSize);

            // ── Resolve speaker status ────────────────────────────────────────
            // The query carries IsSpeaker pre-set by the controller from the JWT claim.
            // We verify it against the database here so a tampered claim cannot bypass
            // the visibility check — the controller flag is advisory only.
            //
            // If currentUserId is null (unauthenticated caller), isSpeaker stays false.
            var isSpeaker = false;
            if (request.CurrentUserId.HasValue)
            {
                isSpeaker = request.IsSpeaker
                    && await _speakerRepository.IsSpeakerForChatRoomAsync(
                           request.CurrentUserId.Value,
                           request.ChatRoomId,
                           cancellationToken);

                _logger.LogInformation(
                    "[GetMessages] Speaker check: IsSpeaker={IsSpeaker} for UserId={UserId}",
                    isSpeaker, request.CurrentUserId.Value);
            }

            // ── Fetch messages ────────────────────────────────────────────────
            // TODO (Step 2 — ChatConversation):
            //   Replace GetMessagesAsync with GetMessagesByConversationAsync once
            //   ConversationId is fully populated on all new rows.
            //   Retain GetMessagesAsync for backward-compatible reads of old rows.
            var messages = await _repository.GetMessagesAsync(
                request.ChatRoomId,
                request.Page,
                request.PageSize,
                request.CurrentUserId,
                isSpeaker);

            _logger.LogInformation(
                "[GetMessages] Returning {Count} message(s) for ChatRoomId={ChatRoomId}",
                messages.Count, request.ChatRoomId);

            return messages;
        }
    }
}