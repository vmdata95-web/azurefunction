using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RoomImageConfiguration : IEntityTypeConfiguration<RoomImage>
    {
        public void Configure(EntityTypeBuilder<RoomImage> builder)
        {
            // ── Table ─────────────────────────────────────────────────────────
            builder.ToTable("room_images");

            // ── Primary Key ───────────────────────────────────────────────────
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            // ── RoomId (FK) ───────────────────────────────────────────────────
            builder.Property(x => x.RoomId)
                .HasColumnName("room_id")
                .IsRequired();

            // ── BlobPath ──────────────────────────────────────────────────────
            builder.Property(x => x.BlobPath)
                .HasColumnName("blob_path")
                .HasMaxLength(500)
                .IsRequired();

            // ── BlobUrl ───────────────────────────────────────────────────────
            builder.Property(x => x.BlobUrl)
                .HasColumnName("blob_url")
                .HasMaxLength(1000)
                .IsRequired();

            // ── OriginalFileName ──────────────────────────────────────────────
            builder.Property(x => x.OriginalFileName)
                .HasColumnName("original_file_name")
                .HasMaxLength(255)
                .IsRequired();

            // ── ContentType ───────────────────────────────────────────────────
            builder.Property(x => x.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(100)
                .IsRequired();

            // ── CreatedAt ─────────────────────────────────────────────────────
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            // ── FK: RoomImage → Room ──────────────────────────────────────────
            builder.HasOne(x => x.Room)
                .WithMany()                         // Room has no nav back to RoomImages
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);  // delete images when room is deleted

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(x => x.RoomId)
                .HasDatabaseName("IX_room_images_room_id");
        }
    }
}
