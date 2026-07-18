namespace Application.Features.RoomImage.Command.UploadRoomImage
{
    /// <summary>
    /// Response returned after a successful image upload.
    /// </summary>
    public class UploadRoomImageResponse
    {
        /// <summary>Unique identifier of the newly created RoomImage record.</summary>
        public Guid ImageId { get; set; }

        /// <summary>The Room this image is linked to.</summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Azure-style virtual path used as the blob name:
        /// e.g.  {roomId}/{imageId}.jpg
        /// </summary>
        public string BlobPath { get; set; } = string.Empty;

        /// <summary>Full public Azure Blob Storage URL ready for use in &lt;img&gt; tags.</summary>
        public string BlobUrl { get; set; } = string.Empty;

        /// <summary>Original file name as supplied by the client.</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>UTC timestamp of when the image was stored.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
