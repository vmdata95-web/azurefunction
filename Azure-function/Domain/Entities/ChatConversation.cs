namespace Domain.Entities;

public class ChatConversation
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    /// <summary>
    /// "public" | "private"
    /// </summary>
    public string ConversationType { get; set; } = null!;

    /// <summary>
    /// The attendee involved in the conversation (null for fully public threads).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The speaker involved in the conversation.
    /// </summary>
    public Guid? SpeakerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    public virtual ChatRoom ChatRoom { get; set; } = null!;

    public virtual User? User { get; set; }

    public virtual Speaker? Speaker { get; set; }

    public virtual ICollection<Message> Messages { get; set; }
        = new List<Message>();
}
