namespace Domain.Entities;

public class Room
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // lobby, auditorium, track, exhibitor, resource
    public string LayoutJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Event Event { get; set; } = null!;
    public virtual ICollection<RoomHotspot> RoomHotspots { get; set; } = new List<RoomHotspot>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
