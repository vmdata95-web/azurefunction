using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IRoomImageRepository"/>.
    /// </summary>
    public class RoomImageRepository : IRoomImageRepository
    {
        private readonly AppDbContext _context;

        public RoomImageRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<bool> RoomExistsAsync(
            Guid roomId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .AnyAsync(r => r.Id == roomId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(
            RoomImage image,
            CancellationToken cancellationToken = default)
        {
            await _context.RoomImages.AddAsync(image, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RoomImage?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.RoomImages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<RoomImage>> GetByRoomIdAsync(
            Guid roomId,
            CancellationToken cancellationToken = default)
        {
            return await _context.RoomImages
                .AsNoTracking()
                .Where(x => x.RoomId == roomId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<RoomImage>> GetByRoomNameAsync(
            string roomName,
            CancellationToken cancellationToken = default)
        {
            // Join RoomImages → Rooms on RoomId, filter by Room.Name (case-insensitive)
            return await _context.RoomImages
                .AsNoTracking()
                .Join(
                    _context.Rooms,
                    img  => img.RoomId,
                    room => room.Id,
                    (img, room) => new { img, room })
                .Where(x => x.room.Name.ToLower() == roomName.ToLower())
                .OrderByDescending(x => x.img.CreatedAt)
                .Select(x => x.img)
                .ToListAsync(cancellationToken);
        }
    }
}
