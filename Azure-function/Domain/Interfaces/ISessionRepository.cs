using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ISessionRepository
    {
        Task<bool> RoomExistsAsync(Guid roomId);
        Task<bool> SpeakerExistsAsync(Guid speakerId);
        Task<bool> IsTimeSlotAvailable(Guid roomId, DateTime start, DateTime end);

        Task<Guid> CreateSessionAsync(Session session);

        Task<Guid?> GetRoomIdByNameAsync(string roomName);
        Task<List<SessionDto>> GetSessionsByRoomIdAsync(Guid roomId);

        Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

        Task<System.IO.Stream?> GetPseudoLiveSegmentAsync(
            Guid sessionId,
            string fileName,
            CancellationToken cancellationToken);

        Task<int> GetCurrentSegmentIndexAsync(
        DateTime startTime,
        int segmentDuration);

        Task<string> GenerateLivePlaylistAsync(
            Guid sessionId,
            int currentIndex,
            int windowSize,
            int segmentDuration,
            string basePath);

        Task<Session?> GetByIdForUpdateAsync(Guid sessionId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
