using System;

namespace Domain.Dto
{
    public class GetLiveSessionResponseDto
    {
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        
        public Guid SpeakerId { get; set; }
        public string SpeakerName { get; set; } = string.Empty;
        
        public string LiveStatus { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
