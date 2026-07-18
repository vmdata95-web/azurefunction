using Domain.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IChatRoomRepository
    {
        Task<Guid> CreateAsync(ChatRoom chatRoom);

        Task<List<ChatRoom>> GetAllAsync();

        Task<Guid> CreateAsync(Message message);

        Task<bool> ChatRoomExistsAsync(Guid chatRoomId);

        Task<bool> UserExistsAsync(Guid userId);
        Task<List<GetMessagesResponse>> GetMessagesAsync(
            Guid chatRoomId,
            int page,
            int pageSize,
            Guid? currentUserId = null,
            bool isSpeaker = false);

        Task AddAsync(Message message, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a single non-deleted message by its primary key.
        /// Returns null when the message does not exist or has been soft-deleted.
        /// </summary>
        Task<Message?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task UpdateAsync(
        Message message,
        CancellationToken cancellationToken);

        Task<Message?> GetByChatRoomIdAsync(Guid chatRoomId);

        /// <summary>
        /// Returns the UserId of every Speaker whose Session belongs to a Room
        /// that is associated with the given ChatRoom's Event.
        ///
        /// Join path: ChatRoom → Event → Room → Session → Speaker → UserId
        ///
        /// Used by SendMessageHandler to target SignalR notifications without
        /// broadcasting the private question to the entire room audience.
        /// </summary>
        Task<List<Guid>> GetSpeakerUserIdsByChatRoomAsync(
            Guid chatRoomId,
            CancellationToken cancellationToken = default);

        Task<bool> IsSpeakerAssignedToChatRoomAsync(
            Guid speakerUserId,
            Guid chatRoomId,
            CancellationToken cancellationToken = default);

        Task<bool> IsSpeakerAssignedToSessionAsync(
            Guid speakerUserId,
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<Guid?> GetEventIdBySessionIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<Guid?> GetSessionIdByChatRoomAsync(
            Guid chatRoomId,
            CancellationToken cancellationToken = default);
    }
}
