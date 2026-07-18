namespace Domain.Entities;

public class ChatRoom
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty; // public / private / helpdesk

    /// <summary>
    /// The Session this ChatRoom is scoped to.
    /// Populated for all new ChatRooms created after the session-scoped architecture was introduced.
    /// Null for legacy ChatRooms — authorization falls back to event-scoped validation for those rows.
    /// When non-null, speaker authorization is validated through this Session directly:
    ///   ChatRoom.SessionId → Session.SpeakerId → Speaker.UserId
    /// </summary>
    public Guid? SessionId { get; set; }

    // Navigation properties
    public virtual Event Event { get; set; } = null!;

    /// <summary>
    /// Navigation to the owning Session. Null for legacy rows where SessionId is not set.
    /// </summary>
    public virtual Session? Session { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
