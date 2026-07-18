using Domain.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IRoomRepository
    {
        Task<bool> EventExistsAsync(Guid eventId);
        Task<Guid> CreateRoomAsync(Room room);
        Task<List<RoomDto>> GetRoomsByEventIdAsync(Guid eventId);
    }
}
