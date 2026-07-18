namespace Domain.Entities;

/// <summary>
/// Represents an image uploaded for a specific Room.
/// The blob is stored in Azure Blob Storage under the path /{RoomId}/{UniqueId}.
/// The full public URL is stored in BlobUrl for fast retrieval.
/// </summary>
public class RoomImage
{
    /// <summary>Unique identifier for this image record (also used as the blob file name).</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the Room this image belongs to.</summary>
    public Guid RoomId { get; set; }

    /// <summary>
    /// Virtual path used when constructing the blob name in Azure:
    /// e.g.  {roomId}/{id}{extension}
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>Full public Azure Blob Storage URL.</summary>
    public string BlobUrl { get; set; } = string.Empty;

    /// <summary>Original file name supplied by the client (for display purposes only).</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME content-type of the uploaded image (e.g. image/jpeg).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Room Room { get; set; } = null!;
}
