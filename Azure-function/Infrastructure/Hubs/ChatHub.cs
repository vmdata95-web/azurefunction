using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Hubs
{
    /// <summary>
    /// The SignalR hub for real-time chat room messaging.
    ///
    /// Architecture note:
    ///   Placed in Infrastructure (not Domain) because it is an ASP.NET Core
    ///   transport concern. Domain only holds the IChatHubService abstraction.
    ///   The Web API (Program.cs) maps this hub at runtime via app.MapHub<ChatHub>.
    ///
    /// Authentication:
    ///   [Authorize] ensures the hub rejects any WebSocket connection that
    ///   does not carry a valid JWT token. The token is extracted from the
    ///   query string (?access_token=...) by Program.cs JwtBearerEvents,
    ///   because browsers cannot send custom Authorization headers during
    ///   a WebSocket handshake.
    ///
    /// Group mechanic:
    ///   Each chat room is a SignalR Group named after its Guid (chatRoomId.ToString()).
    ///   Clients call JoinRoom() after connecting. Only then does
    ///   Clients.Group(roomId).SendAsync() reach them.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            _logger.LogInformation(
                "[SignalR] Client connected. ConnectionId={ConnectionId} UserId={UserId}",
                Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;

            if (exception is not null)
            {
                _logger.LogWarning(exception,
                    "[SignalR] Client disconnected with error. ConnectionId={ConnectionId} UserId={UserId}",
                    Context.ConnectionId, userId);
            }
            else
            {
                _logger.LogInformation(
                    "[SignalR] Client disconnected cleanly. ConnectionId={ConnectionId} UserId={UserId}",
                    Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ---------------------------------------------------------------
        // Group subscription methods — called by the frontend client
        // ---------------------------------------------------------------

        /// <summary>
        /// Adds the caller's connection to the SignalR group for the given room.
        /// The frontend MUST call this immediately after the hub connection starts,
        /// otherwise Clients.Group(roomId).SendAsync() will not reach this client.
        /// </summary>
        /// <param name="roomId">The chat room Guid as a string.</param>
        public async Task JoinRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                _logger.LogWarning(
                    "[SignalR] JoinRoom called with empty roomId. ConnectionId={ConnectionId}",
                    Context.ConnectionId);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            _logger.LogInformation(
                "[SignalR] ConnectionId={ConnectionId} joined room={RoomId}",
                Context.ConnectionId, roomId);

            // Notify the caller that they successfully joined (optional confirmation)
            await Clients.Caller.SendAsync("JoinedRoom", roomId);
        }

        /// <summary>
        /// Removes the caller's connection from the given room group.
        /// Call this when the user navigates away from the chat room.
        /// </summary>
        /// <param name="roomId">The chat room Guid as a string.</param>
        public async Task LeaveRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            _logger.LogInformation(
                "[SignalR] ConnectionId={ConnectionId} left room={RoomId}",
                Context.ConnectionId, roomId);

            await Clients.Caller.SendAsync("LeftRoom", roomId);
        }
    }
}
