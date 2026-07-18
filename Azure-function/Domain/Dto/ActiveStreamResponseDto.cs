using System;

namespace Domain.Dto
{
    public class ActiveStreamResponseDto
    {
        public Guid SessionId { get; set; }
        
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        
        public Guid SpeakerId { get; set; }
        public string SpeakerName { get; set; } = string.Empty;
        
        public string StreamStatus { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
    }
}
