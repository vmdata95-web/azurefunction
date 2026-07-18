using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAgendaRepository
    {
        Task<List<AgendaResponse>> GetAgendaAsync(Guid eventId);
    }


    public class AgendaResponse
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public List<SessionDto> Sessions { get; set; } = new();
    }

    public class SessionDto
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string SpeakerName { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int? TotalSegments { get; set; }
    }
}
