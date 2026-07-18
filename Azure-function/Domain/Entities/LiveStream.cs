using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a live stream session tied to a room and speaker.
/// A session must already exist (via CreateSession) before it can go live.
/// </summary>
public class LiveStream
{
    public Guid Id { get; set; }

    /// <summary>
    /// The session this live stream belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The room in which the live stream is broadcast.
    /// </summary>
    public Guid RoomId { get; set; }

    /// <summary>
    /// The speaker who owns/started this live stream.
    /// </summary>
    public Guid SpeakerId { get; set; }

    /// <summary>
    /// Current lifecycle status of the live stream.
    /// </summary>
    public LiveStreamStatus Status { get; set; } = LiveStreamStatus.Scheduled;

    /// <summary>
    /// UTC timestamp when the live stream actually started broadcasting.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the live stream ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Optional streaming key / RTMP token that can be passed to streaming infra.
    /// </summary>
    public string StreamKey { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary JSON metadata (e.g. viewer count snapshot, CDN endpoint).
    /// </summary>
    public string Metadata { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────
    public virtual Session Session { get; set; } = null!;
    public virtual Room    Room    { get; set; } = null!;
    public virtual Speaker Speaker { get; set; } = null!;
}
