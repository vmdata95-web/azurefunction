namespace Domain.Entities;

public class Exhibitor
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Navigation properties
    public virtual Event Event { get; set; } = null!;
}
