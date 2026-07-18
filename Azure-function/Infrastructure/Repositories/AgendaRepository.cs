using Domain.Interfaces;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AgendaRepository : IAgendaRepository
    {
        private readonly AppDbContext _context;

        public AgendaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AgendaResponse>> GetAgendaAsync(Guid eventId)
        {
            return await _context.Rooms
                .Where(r => r.EventId == eventId)
                .Select(r => new AgendaResponse
                {
                    RoomId = r.Id,
                    RoomName = r.Name,
                    Type = r.Type,

                    Sessions = r.Sessions
                        .OrderBy(s => s.StartTime)
                        .Select(s => new SessionDto
                        {
                            SessionId = s.Id,
                            Title = s.Title,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            VideoUrl = s.VideoUrl,
                            SpeakerName = s.Speaker.User.Name,
                            Company = s.Speaker.Company
                        }).ToList()
                })
                .ToListAsync();
        }
    }
}
