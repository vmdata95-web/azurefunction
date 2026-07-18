namespace Application.Features.RoomImage.Query.GetRoomImage
{
    /// <summary>
    /// DTO for a single image record returned by the GET API.
    /// </summary>
    public class RoomImageDto
    {
        public Guid   ImageId          { get; set; }
        public Guid   RoomId           { get; set; }
        public string BlobPath         { get; set; } = string.Empty;
        public string BlobUrl          { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType      { get; set; } = string.Empty;
        public DateTime CreatedAt      { get; set; }
    }

    /// <summary>
    /// Top-level response returned by the GET /api/roomimage endpoint.
    /// </summary>
    public class GetRoomImageResponse
    {
        /// <summary>
        /// The list of matching images.
        /// Contains exactly one item when queried by ImageId;
        /// contains zero-to-many items when queried by RoomId.
        /// </summary>
        public List<RoomImageDto> Images { get; set; } = new();
    }
}
