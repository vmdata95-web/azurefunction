namespace Domain.Entities;

public class UserEvent
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }   // nullable
    public Guid? EventId { get; set; }  // nullable

    public DateTime? RegisteredAt { get; set; }
    public bool? IsCheckedIn { get; set; }

    // Navigation
    public User? User { get; set; }
    public Event? Event { get; set; }
}
