using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            // ✅ Table Name
            builder.ToTable("rooms");

            // ✅ Primary Key
            builder.HasKey(x => x.Id);

            // ✅ Id Default (SQL Server)
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // ✅ EventId (FK)
            builder.Property(x => x.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            // ✅ Name
            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            // ✅ Type
            builder.Property(x => x.Type)
                .HasColumnName("type")
                .HasMaxLength(50)
                .IsRequired();

            // ✅ Layout JSON
            builder.Property(x => x.LayoutJson)
                .HasColumnName("layout_json")
                .IsRequired();

            // ✅ CreatedAt
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            // ✅ Relationship (Event → Rooms)
            builder.HasOne(x => x.Event)
                .WithMany(e => e.Rooms)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade); // 🔥 important

            // ✅ JSON CHECK CONSTRAINT
            builder.HasCheckConstraint(
                "CK_rooms_layout_json",
                "ISJSON([layout_json]) > 0"
            );
        }
    }
}