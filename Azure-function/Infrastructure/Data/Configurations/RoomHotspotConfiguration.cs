using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    public class RoomHotspotConfiguration : IEntityTypeConfiguration<RoomHotspot>
    {
        public void Configure(EntityTypeBuilder<RoomHotspot> builder)
        {
            builder.ToTable("room_hotspots");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Room)
                .WithMany(r => r.RoomHotspots)
                .HasForeignKey(x => x.RoomId);

            builder.Property(x => x.Name).HasMaxLength(100);

            builder.Property(x => x.ActionType)
                .HasColumnName("action_type")
                .HasMaxLength(50);

            builder.Property(x => x.ActionValue)
                .HasColumnName("action_value");

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
