using Domain.Dto;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EventExistsAsync(Guid eventId)
        {
            return await _context.Events.AnyAsync(e => e.Id == eventId);
        }

        public async Task<Guid> CreateRoomAsync(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return room.Id;
        }

        public async Task<List<RoomDto>> GetRoomsByEventIdAsync(Guid eventId)
        {
            return await _context.Rooms
                .Where(r => r.EventId == eventId)
                 .OrderBy(r => r.Name)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                })
                .ToListAsync();
        }
    }
}
