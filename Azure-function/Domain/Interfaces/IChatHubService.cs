namespace Domain.Interfaces
{
    /// <summary>
    /// Abstraction over the SignalR transport layer.
    /// The Application layer depends only on this interface;
    /// the concrete ChatHubService lives in Infrastructure.
    /// </summary>
    public interface IChatHubService
    {
        /// <summary>
        /// Broadcasts a payload to ALL clients in the given chat room.
        /// Use for public messages only.
        /// </summary>
        Task SendMessageToRoomAsync(Guid chatRoomId, object data);

        /// <summary>
        /// Sends a payload to a SINGLE user (identified by their UserId,
        /// which must match the NameIdentifier claim used as UserIdentifier
        /// in the SignalR hub). Use for private replies only — never broadcast.
        /// </summary>
        Task SendPrivateMessageAsync(Guid userId, object data);

        /// <summary>
        /// Delivers a new-question notification to a specific speaker's connections.
        /// Uses Clients.User(speakerUserId) — never touches the room group.
        ///
        /// Event name: "NewQuestion"
        /// Frontend speaker listener: connection.on("NewQuestion", (payload) => { ... })
        ///
        /// Call this once per speaker when an attendee submits a question.
        /// Payload should contain: MessageId, ChatRoomId, UserId, UserName, Message, CreatedAt.
        /// </summary>
        Task NotifySpeakerAsync(Guid speakerUserId, object payload);
    }
}
