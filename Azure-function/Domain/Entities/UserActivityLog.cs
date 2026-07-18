using Domain.Enums;

namespace Domain.Entities;

public class UserActivityLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EventId { get; set; }

    public UserActivityAction Action { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string Metadata { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;
}