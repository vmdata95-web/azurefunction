using Domain.Entities;
using Microsoft.EntityFrameworkCore;
//using System.Data.Entity;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<RoomHotspot> RoomHotspots => Set<RoomHotspot>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<LiveStream> LiveStreams => Set<LiveStream>();
    public DbSet<Exhibitor> Exhibitors => Set<Exhibitor>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<VideoJob> VideoJobs => Set<VideoJob>();
    public DbSet<EmailQueue> EmailQueues => Set<EmailQueue>();
    public DbSet<UserCredential> UserCredential => Set<UserCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}