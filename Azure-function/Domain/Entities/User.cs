namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    //public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // admin, user, speaker, exhibitor

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Domain.Enums.UserRole RoleEnum
    {
        get
        {
            if (Enum.TryParse<Domain.Enums.UserRole>(Role, true, out var r))
                return r;
            return Domain.Enums.UserRole.User; // default fallback
        }
        set
        {
            Role = value.ToString();
        }
    }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Guid? SessionId { get; set; }
    public string? Designation { get; set; }
    public string? CompanyName { get; set; }
    public string? MobileNo { get; set; }
    public string? Country { get; set; }

    public string? Number_Of_Employees { get; set; }

    public int? Registerfrom { get; set; }

    public string? IpAddress { get; set; }

    // Navigation properties
    public virtual ICollection<UserEvent> UserEvents { get; set; } = new List<UserEvent>();
    public virtual Speaker? Speaker { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<TicketMessage> TicketMessages { get; set; } = new List<TicketMessage>();
    public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
    public virtual UserCredential? UserCredential { get; set; }
}
