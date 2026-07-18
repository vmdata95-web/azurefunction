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
    public class ExhibitorConfiguration
    : IEntityTypeConfiguration<Exhibitor>
    {
        public void Configure(EntityTypeBuilder<Exhibitor> builder)
        {
            builder.ToTable("exhibitors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.EventId)
                .HasColumnName("event_id");

            builder.Property(x => x.Name)
                .HasColumnName("name");

            builder.Property(x => x.url)
                .HasColumnName("url");

            builder.Property(x => x.Description)
                .HasColumnName("description");

            builder.Property(x => x.Website)
                .HasColumnName("website");
        }
    }
}
