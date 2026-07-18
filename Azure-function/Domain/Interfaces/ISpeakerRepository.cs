using Domain.Dto;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISpeakerRepository
    {
        Task<bool> UserExistsAsync(Guid userId);
        Task<bool> SpeakerExistsAsync(Guid userId);
        Task<Guid> CreateSpeakerAsync(Speaker speaker);
        Task UpdateUserRoleAsync(Guid userId, string role);
        Task<List<SpeakerDto>> GetSpeakersAsync();

        /// <summary>
        /// Returns true when the given userId belongs to a Speaker who has at least
        /// one Session in a Room that is under the Event that owns the given chatRoomId.
        ///
        /// Join path: ChatRoom → EventId → Event → Room → Session → Speaker → UserId
        ///
        /// Used by GetMessagesAsync to grant speakers visibility of ALL private
        /// questions in their room without broadcasting them to attendees.
        /// </summary>
        Task<bool> IsSpeakerForChatRoomAsync(
            Guid userId,
            Guid chatRoomId,
            CancellationToken cancellationToken = default);
    }
}
