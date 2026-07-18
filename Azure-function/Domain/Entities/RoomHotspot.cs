namespace Domain.Entities;

public class RoomHotspot
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string ActionType { get; set; } = string.Empty; // navigate / link / join_room
    public string ActionValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
}
