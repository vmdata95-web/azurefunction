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
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("tickets");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Subject).HasMaxLength(200);
            builder.Property(x => x.Status).HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.HasOne(x => x.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.Event)
                .WithMany(e => e.Tickets)
                .HasForeignKey(x => x.EventId);
        }
    }
}
