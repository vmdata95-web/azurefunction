using Domain.Dto;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="ILiveStreamRepository"/>.
    /// All DB access goes through <see cref="AppDbContext"/>.
    /// </summary>
    public class LiveStreamRepository : ILiveStreamRepository
    {
        private readonly AppDbContext _context;

        public LiveStreamRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── Existence / validation queries ────────────────────────────────

        public async Task<bool> SessionExistsAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .AnyAsync(s => s.Id == sessionId, cancellationToken);
        }

        public async Task<bool> RoomExistsAsync(
            Guid roomId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .AnyAsync(r => r.Id == roomId, cancellationToken);
        }

        public async Task<bool> SpeakerExistsAsync(
            Guid speakerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Speakers
                .AnyAsync(s => s.Id == speakerId, cancellationToken);
        }

        public async Task<bool> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<bool> IsSessionAlreadyLiveAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.LiveStreams
                .AnyAsync(ls => ls.SessionId == sessionId
                             && ls.Status == LiveStreamStatus.Live,
                          cancellationToken);
        }

        // ── Write operations ──────────────────────────────────────────────

        public async Task<Guid> StartLiveStreamAsync(
            LiveStream liveStream,
            CancellationToken cancellationToken = default)
        {
            // Persist the new live stream record
            _context.LiveStreams.Add(liveStream);

            // Also update the parent session's status to "live"
            // so consumers of the Sessions table see the correct state.
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == liveStream.SessionId, cancellationToken);

            if (session is not null)
            {
                session.Status = LiveStreamStatus.Live.ToString().ToLowerInvariant();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return liveStream.Id;
        }

        public async Task<(Guid LiveStreamId, DateTime EndedAt)> StopLiveStreamAsync(
            Guid sessionId,
            Guid speakerId,
            DateTime endedAt,
            CancellationToken cancellationToken = default)
        {
            // Fetch the active live stream record
            var liveStream = await _context.LiveStreams
                .FirstOrDefaultAsync(ls => ls.SessionId == sessionId
                                        && ls.Status == LiveStreamStatus.Live,
                                     cancellationToken)
                ?? throw new InvalidOperationException(
                    $"No active live stream found for session '{sessionId}'.");

            // Transition to Ended
            liveStream.Status    = LiveStreamStatus.Ended;
            liveStream.EndedAt   = endedAt;
            liveStream.UpdatedAt = endedAt;

            // Update the parent session's status to "ended"
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session is not null)
            {
                session.Status = LiveStreamStatus.Ended.ToString().ToLowerInvariant();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return (liveStream.Id, endedAt);
        }

        public async Task RecordUserActivityAsync(
            Guid userId,
            Guid sessionId,
            UserActivityAction action,
            DateTime timestamp,
            CancellationToken cancellationToken = default)
        {
            // Resolve the event associated with this session's room
            var session = await _context.Sessions
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session is null)
                return; // Session already validated by handler — skip silently

            var log = new UserActivityLog
            {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                EventId   = session.Room.EventId,
                Action    = action,
                RoomName  = session.Room.Name,
                Metadata  = $"{{\"sessionId\":\"{sessionId}\"}}",
                CreatedAt = timestamp
            };

            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> SaveLiveChatMessageAsync(
            Guid sessionId,
            Guid userId,
            string messageText,
            DateTime sentAt,
            CancellationToken cancellationToken = default)
        {
            // Resolve the room + event for this session so we can find/create a ChatRoom
            var session = await _context.Sessions
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session is null)
                throw new InvalidOperationException($"Session '{sessionId}' not found.");

            // Find or create a "live" ChatRoom for this event
            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(cr => cr.EventId == session.Room.EventId
                                        && cr.Type    == "live",
                                     cancellationToken);

            if (chatRoom is null)
            {
                chatRoom = new ChatRoom
                {
                    Id      = Guid.NewGuid(),
                    EventId = session.Room.EventId,
                    Type    = "live"
                };
                _context.ChatRooms.Add(chatRoom);
                // Save so we have a valid ChatRoomId before adding the message
                await _context.SaveChangesAsync(cancellationToken);
            }

            var message = new Message
            {
                Id          = Guid.NewGuid(),
                ChatRoomId  = chatRoom.Id,
                UserId      = userId,
                MessageText = messageText,
                MessageType = "public",
                CreatedAt   = sentAt
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);

            return message.Id;
        }

        // ── Read operations ───────────────────────────────────────────────

        public async Task<LiveStreamDto?> GetLiveStreamBySessionIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.LiveStreams
                .AsNoTracking()
                .Where(ls => ls.SessionId == sessionId)
                .OrderByDescending(ls => ls.StartedAt)
                .Select(ls => new LiveStreamDto
                {
                    LiveStreamId = ls.Id,
                    SessionId    = ls.SessionId,
                    RoomId       = ls.RoomId,
                    SpeakerId    = ls.SpeakerId,
                    SessionTitle = ls.Session.Title,
                    RoomName     = ls.Room.Name,
                    Status       = ls.Status.ToString(),
                    StreamKey    = ls.StreamKey,
                    StartedAt    = ls.StartedAt,
                    EndedAt      = ls.EndedAt,
                    CreatedAt    = ls.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<GetLiveSessionResponseDto?> GetLiveSessionDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.LiveStreams
                .AsNoTracking()
                .Where(ls => ls.SessionId == sessionId)
                .OrderByDescending(ls => ls.StartedAt)
                .Select(ls => new GetLiveSessionResponseDto
                {
                    SessionId    = ls.SessionId,
                    SessionTitle = ls.Session.Title,
                    RoomId       = ls.RoomId,
                    RoomName     = ls.Room.Name,
                    SpeakerId    = ls.SpeakerId,
                    SpeakerName  = ls.Speaker.User.Name,
                    LiveStatus   = ls.Status.ToString(),
                    StartedAt    = ls.StartedAt,
                    EndedAt      = ls.EndedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<ActiveStreamResponseDto>> GetActiveStreamsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.LiveStreams
                .AsNoTracking()
                .Where(ls => ls.Status == LiveStreamStatus.Live)
                .OrderByDescending(ls => ls.StartedAt)
                .Select(ls => new ActiveStreamResponseDto
                {
                    SessionId    = ls.SessionId,
                    RoomId       = ls.RoomId,
                    RoomName     = ls.Room.Name,
                    SpeakerId    = ls.SpeakerId,
                    SpeakerName  = ls.Speaker.User.Name,
                    StreamStatus = ls.Status.ToString(),
                    StartedAt    = ls.StartedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
