using Domain.Dto;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly AppDbContext _context;

        public ChatRoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateAsync(ChatRoom chatRoom)
        {
            chatRoom.Id = Guid.NewGuid();

            await _context.ChatRooms.AddAsync(chatRoom);

            await _context.SaveChangesAsync();

            return chatRoom.Id;
        }

        public async Task<List<ChatRoom>> GetAllAsync()
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<bool> ChatRoomExistsAsync(Guid chatRoomId)
        {
            return await _context.ChatRooms
                .AnyAsync(x => x.Id == chatRoomId);
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users
                .AnyAsync(x => x.Id == userId);
        }

        public async Task<Guid> CreateAsync(Message message)
        {
            message.Id = Guid.NewGuid();

            message.CreatedAt = DateTime.UtcNow;

            await _context.Messages.AddAsync(message);

            await _context.SaveChangesAsync();

            return message.Id;
        }

        public async Task<List<GetMessagesResponse>> GetMessagesAsync(
            Guid chatRoomId,
            int page,
            int pageSize,
            Guid? currentUserId = null,
            bool isSpeaker = false)
        {
            return await _context.Messages
                .AsNoTracking()
                .Where(x => x.ChatRoomId == chatRoomId && !x.IsDeleted)
                .Where(x =>
                    // ── Visibility rules ──────────────────────────────────────
                    // PUBLIC messages: always visible to everyone.
                    x.MessageType != "private"

                    // PRIVATE messages — three cases that grant access:

                    // 1. The caller is the message author (attendee sees their own question).
                    || (currentUserId != null && x.UserId == currentUserId)

                    // 2. The caller is a speaker for this room.
                    //    ReceiverUserId is null on attendee questions by design (not a DM),
                    //    so we cannot use ReceiverUserId == currentUserId to identify speakers.
                    //    Instead, isSpeaker is pre-resolved by the handler.
                    || isSpeaker

                    // 3. Legacy: private speaker replies where ReceiverUserId was explicitly set
                    //    (backward compatibility for rows created before this architecture).
                    || (currentUserId != null && x.ReceiverUserId == currentUserId)
                )
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new GetMessagesResponse
                {
                    Id               = x.Id,
                    ChatRoomId       = x.ChatRoomId,
                    UserId           = x.UserId,
                    Name             = x.User.Name,
                    Message          = x.MessageText,
                    CreatedAt        = x.CreatedAt,
                    MessageType      = x.MessageType,
                    ReceiverUserId   = x.ReceiverUserId,
                    ReplyToMessageId = x.ReplyToMessageId,
                    SenderType       = x.SenderType
                })
                .ToListAsync();
        }

        public async Task UpdateAsync(
            Message message,
            CancellationToken cancellationToken)
        {
            _context.Messages.Update(message);

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<Message?> GetByChatRoomIdAsync(Guid chatRoomId)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(x =>
                    x.ChatRoomId == chatRoomId);
        }

        public async Task AddAsync(Message message, CancellationToken cancellationToken)
        {
            await _context.Messages.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Message?> GetMessageByIdAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == messageId && !x.IsDeleted,
                    cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Guid>> GetSpeakerUserIdsByChatRoomAsync(
            Guid chatRoomId,
            CancellationToken cancellationToken = default)
        {
            var chatRoom = await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Id == chatRoomId)
                .Select(cr => new { cr.SessionId })
                .FirstOrDefaultAsync(cancellationToken);

            if (chatRoom is null || !chatRoom.SessionId.HasValue)
                return new List<Guid>();

            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Id == chatRoom.SessionId.Value)
                .Join(
                    _context.Speakers,
                    s  => s.SpeakerId,
                    sp => sp.Id,
                    (s, sp) => sp.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsSpeakerAssignedToChatRoomAsync(
            Guid speakerUserId,
            Guid chatRoomId,
            CancellationToken cancellationToken = default)
        {
            var chatRoom = await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Id == chatRoomId)
                .Select(cr => new { cr.SessionId })
                .FirstOrDefaultAsync(cancellationToken);

            if (chatRoom is null || !chatRoom.SessionId.HasValue)
                return false;

            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Id == chatRoom.SessionId.Value)
                .Join(
                    _context.Speakers,
                    s  => s.SpeakerId,
                    sp => sp.Id,
                    (s, sp) => sp.UserId)
                .AnyAsync(uid => uid == speakerUserId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsSpeakerAssignedToSessionAsync(
            Guid speakerUserId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Id == sessionId)
                .Join(
                    _context.Speakers,
                    s  => s.SpeakerId,
                    sp => sp.Id,
                    (s, sp) => sp.UserId)
                .AnyAsync(uid => uid == speakerUserId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Guid?> GetEventIdBySessionIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Id == sessionId)
                .Select(s => (Guid?)s.Room.EventId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Guid?> GetSessionIdByChatRoomAsync(
            Guid chatRoomId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Id == chatRoomId)
                .Select(cr => cr.SessionId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}