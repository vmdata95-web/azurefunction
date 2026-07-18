using Domain.Interfaces;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Concrete implementation of IChatHubService.
    ///
    /// This service is the SINGLE entry point for all SignalR broadcasts
    /// originating from application handlers. Handlers inject IChatHubService
    /// (the Domain abstraction) and never touch IHubContext directly,
    /// keeping the Application layer free of ASP.NET Core transport dependencies.
    ///
    /// Error isolation:
    ///   Hub broadcast errors are logged but deliberately swallowed so that
    ///   a transient SignalR issue never causes the HTTP API response to fail.
    ///   The message is already persisted to the database at this point, so
    ///   the operation is considered successful even if real-time delivery fails.
    ///
    /// Private message routing:
    ///   SendPrivateMessageAsync uses Clients.User(userId) which targets all
    ///   active connections belonging to that user. SignalR maps userId via
    ///   IUserIdProvider → the sub/NameIdentifier claim on the JWT.
    ///   This means ONLY the intended recipient receives the frame; the payload
    ///   is never written to the room group.
    /// </summary>
    public class ChatHubService : IChatHubService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatHubService> _logger;

        public ChatHubService(
            IHubContext<ChatHub> hubContext,
            ILogger<ChatHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Broadcasts a message payload to all SignalR clients that have
        /// joined the group for the given chat room.
        ///
        /// Event name: "ReceiveMessage"
        /// Frontend listener: connection.on("ReceiveMessage", (payload) => { ... })
        /// </summary>
        public async Task SendMessageToRoomAsync(Guid chatRoomId, object data)
        {
            try
            {
                await _hubContext.Clients
                    .Group(chatRoomId.ToString())
                    .SendAsync("ReceiveMessage", data);

                _logger.LogInformation(
                    "[SignalR] Broadcast 'ReceiveMessage' to room={RoomId}",
                    chatRoomId);
            }
            catch (Exception ex)
            {
                // Log the failure but do NOT re-throw.
                // The database write has already committed — we don't want
                // a WebSocket failure to roll back the user's perceived success.
                _logger.LogError(ex,
                    "[SignalR] Failed to broadcast 'ReceiveMessage' to room={RoomId}",
                    chatRoomId);
            }
        }

        /// <summary>
        /// Sends a payload to a SINGLE user's connections only.
        /// Uses Clients.User(userId) which internally resolves to every
        /// connection whose IUserIdProvider returns the given userId string.
        ///
        /// Event name: "ReceivePrivateMessage"
        /// Frontend listener: connection.on("ReceivePrivateMessage", (payload) => { ... })
        ///
        /// This method MUST be used for private speaker replies.
        /// It guarantees zero leakage to other room participants.
        /// </summary>
        public async Task SendPrivateMessageAsync(Guid userId, object data)
        {
            try
            {
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("ReceivePrivateMessage", data);

                _logger.LogInformation(
                    "[SignalR] Private 'ReceivePrivateMessage' sent to UserId={UserId}",
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SignalR] Failed to send 'ReceivePrivateMessage' to UserId={UserId}",
                    userId);
            }
        }
        /// <summary>
        /// Sends a new-question notification to a single speaker's connections.
        /// Uses Clients.User(speakerUserId) — the payload never reaches the room group.
        ///
        /// Event name: "NewQuestion"
        /// Frontend speaker listener: connection.on("NewQuestion", (payload) => { ... })
        ///
        /// Error isolation: failures are logged but swallowed — the question is already
        /// persisted, so a transient SignalR failure must not fail the HTTP response.
        /// </summary>
        public async Task NotifySpeakerAsync(Guid speakerUserId, object payload)
        {
            try
            {
                await _hubContext.Clients
                    .User(speakerUserId.ToString())
                    .SendAsync("NewQuestion", payload);

                _logger.LogInformation(
                    "[SignalR] 'NewQuestion' notification sent to SpeakerUserId={SpeakerUserId}",
                    speakerUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SignalR] Failed to send 'NewQuestion' to SpeakerUserId={SpeakerUserId}",
                    speakerUserId);
            }
        }
    }
}
