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
    /// <summary>
    /// EF Core Fluent API configuration for the EmailQueue table.
    /// Mirrors the column-naming conventions used across the rest of the project
    /// (snake_case column names, explicit max-lengths, default values on the DB side).
    /// </summary>
    public class EmailQueueConfiguration : IEntityTypeConfiguration<EmailQueue>
    {
        public void Configure(EntityTypeBuilder<EmailQueue> builder)
        {
            builder.ToTable("EmailQueue");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .HasDefaultValueSql("NEWID()");

            builder.Property(x => x.UserId)
                .HasColumnName("UserId");

            builder.Property(x => x.Email)
                .HasColumnName("Email")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Subject)
                .HasColumnName("Subject")
                .HasMaxLength(500)
                .IsRequired();

            // Body can hold full HTML — use MAX to avoid truncation of large templates.
            builder.Property(x => x.Body)
                .HasColumnName("Body")
                .HasColumnType("nvarchar(MAX)")
                .IsRequired();

            builder.Property(x => x.IsHtml)
                .HasColumnName("IsHtml")
                .HasDefaultValue(true)
                .IsRequired();

            // Status stored as int: 0=Pending, 1=Processing, 2=Sent, 3=Failed
            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.RetryCount)
                .HasColumnName("RetryCount")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("ErrorMessage")
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(x => x.SentAt)
                .HasColumnName("SentAt");

            // Index on Status + CreatedAt: the background service always queries
            // WHERE Status = 0 ORDER BY CreatedAt; this composite index covers it.
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_EmailQueue_Status_CreatedAt");
        }
    }

}
