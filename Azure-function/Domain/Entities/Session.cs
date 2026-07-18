namespace Domain.Entities;

public class Session
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SpeakerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total number of HLS segments available for this pseudo-live stream.
    /// Null indicates unknown or legacy sessions without segment count metadata.
    /// Used for VOD fallback logic once live playback window passes the final segment.
    /// </summary>
    public int? TotalHlsSegments { get; set; }

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual Speaker Speaker { get; set; } = null!;
}
