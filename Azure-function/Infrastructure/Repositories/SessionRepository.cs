using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
namespace Infrastructure.Repositories
{


    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public SessionRepository(AppDbContext context, IBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<bool> RoomExistsAsync(Guid roomId)
        {
            return await _context.Rooms.AnyAsync(x => x.Id == roomId);
        }

        public async Task<bool> SpeakerExistsAsync(Guid speakerId)
        {
            return await _context.Speakers.AnyAsync(x => x.Id == speakerId);
        }

        public async Task<bool> IsTimeSlotAvailable(Guid roomId, DateTime start, DateTime end)
        {
            return !await _context.Sessions.AnyAsync(x =>
                x.RoomId == roomId &&
                x.StartTime < end &&
                x.EndTime > start
            );
        }

        public async Task<Guid> CreateSessionAsync(Session session)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session.Id;
        }

        public async Task<Guid?> GetRoomIdByNameAsync(string roomName)
        {
            return await _context.Rooms
                .Where(r => r.Name == roomName)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync();
        }


        public async Task<List<SessionDto>> GetSessionsByRoomIdAsync(Guid roomId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.RoomId == roomId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return sessions.Select(s => new SessionDto
            {
                SessionId = s.Id,
                Title = s.Title,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                VideoUrl = !string.IsNullOrEmpty(s.VideoUrl)
                    ? s.VideoUrl.Split('/').Last()
                    : string.Empty,
                TotalSegments = s.TotalHlsSegments,
                Status = s.Status

            }).ToList();
        }

        public async Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            return await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }




        public async Task<System.IO.Stream?> GetPseudoLiveSegmentAsync(
            Guid sessionId,
            string fileName,
            CancellationToken cancellationToken)
        {
            var blobName = $"session-live-video/{sessionId}/{fileName}";
            return await _blobStorageService.GetBlobStreamAsync(blobName, cancellationToken);
        }

        public Task<int> GetCurrentSegmentIndexAsync(
            DateTime startTime,
            int segmentDuration)
        {
            return Task.FromResult(0);
        }

        public Task<string> GenerateLivePlaylistAsync(
            Guid sessionId,
            int currentIndex,
            int windowSize,
            int segmentDuration,
            string basePath)
        {
            return Task.FromResult(string.Empty);
        }

        public async Task<Session?> GetByIdForUpdateAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            return await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
