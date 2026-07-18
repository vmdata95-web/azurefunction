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
    public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
    {
        public void Configure(EntityTypeBuilder<TicketMessage> builder)
        {
            builder.ToTable("ticket_messages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                .HasColumnName("message");

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.HasOne(x => x.Ticket)
                .WithMany(t => t.TicketMessages)
                .HasForeignKey(x => x.TicketId);

            builder.HasOne(x => x.Sender)
                .WithMany(u => u.TicketMessages)
                .HasForeignKey(x => x.SenderId);
        }
    }
}
