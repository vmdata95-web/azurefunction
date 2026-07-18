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
    public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder.ToTable("resources");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).HasMaxLength(200);

            builder.Property(x => x.Type).HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.HasOne(x => x.Event)
                .WithMany(e => e.Resources)
                .HasForeignKey(x => x.EventId);
        }
    }
}
