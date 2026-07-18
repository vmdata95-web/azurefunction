using Domain.Dto;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository contract for all live stream persistence operations.
    /// Consumed by Application layer command/query handlers.
    /// </summary>
    public interface ILiveStreamRepository
    {
        // ── Existence / validation queries ────────────────────────────────

        /// <summary>Returns true if a session with the given id exists.</summary>
        Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>Returns true if a room with the given id exists.</summary>
        Task<bool> RoomExistsAsync(Guid roomId, CancellationToken cancellationToken = default);

        /// <summary>Returns true if a speaker with the given id exists.</summary>
        Task<bool> SpeakerExistsAsync(Guid speakerId, CancellationToken cancellationToken = default);

        /// <summary>Returns true if a user with the given id exists.</summary>
        Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true if the session already has an active (Live) live stream record.
        /// Used to enforce the "no duplicate live stream" rule.
        /// </summary>
        Task<bool> IsSessionAlreadyLiveAsync(Guid sessionId, CancellationToken cancellationToken = default);

        // ── Write operations ──────────────────────────────────────────────

        /// <summary>
        /// Persists a new LiveStream record and updates the parent Session status.
        /// Returns the new LiveStream's id.
        /// </summary>
        Task<Guid> StartLiveStreamAsync(LiveStream liveStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends the active live stream for the given session.
        /// Sets Status = Ended, stamps EndedAt, and updates Session.Status.
        /// Returns a minimal result carrying the LiveStreamId.
        /// </summary>
        Task<(Guid LiveStreamId, DateTime EndedAt)> StopLiveStreamAsync(
            Guid sessionId,
            Guid speakerId,
            DateTime endedAt,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a join (Enter) or leave (Out) activity for a user in the
        /// UserActivityLogs table, tied to the room associated with the session.
        /// </summary>
        Task RecordUserActivityAsync(
            Guid userId,
            Guid sessionId,
            UserActivityAction action,
            DateTime timestamp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a live chat message using the existing Message entity
        /// linked to the ChatRoom associated with the session's room/event.
        /// Returns the new Message's id.
        /// </summary>
        Task<Guid> SaveLiveChatMessageAsync(
            Guid sessionId,
            Guid userId,
            string messageText,
            DateTime sentAt,
            CancellationToken cancellationToken = default);

        // ── Read operations ───────────────────────────────────────────────

        /// <summary>Returns the live stream DTO for a given session, or null.</summary>
        Task<LiveStreamDto?> GetLiveStreamBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>Returns detailed information about a live session.</summary>
        Task<GetLiveSessionResponseDto?> GetLiveSessionDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>Returns a list of all active/live streams.</summary>
        Task<IEnumerable<ActiveStreamResponseDto>> GetActiveStreamsAsync(CancellationToken cancellationToken = default);
    }
}
