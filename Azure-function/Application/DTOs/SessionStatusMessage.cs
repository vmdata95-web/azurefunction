using System;

namespace Application.DTOs
{
    public class SessionStatusMessage
    {
        public Guid SessionId { get; set; }
        public string Action { get; set; } = string.Empty; // "Live" or "Ended"
    }
}
