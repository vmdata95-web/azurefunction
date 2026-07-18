using MediatR;

namespace Application.Features.RoomImage.Query.GetRoomImage
{
    /// <summary>
    /// Query to fetch image(s) associated with a Room.
    ///
    /// Lookup behaviour (first match wins):
    ///   1. If <see cref="ImageId"/> is provided → returns that single image.
    ///   2. If <see cref="RoomId"/> is provided  → returns all images for that room (by GUID).
    ///   3. If <see cref="RoomName"/> is provided → returns all images for the room whose
    ///      Name matches (case-insensitive), e.g. "Login".
    /// At least one of the three must be non-empty (enforced by the validator).
    /// </summary>
    public class GetRoomImageQuery : IRequest<GetRoomImageResponse>
    {
        /// <summary>Optional. Fetch a specific image by its unique ID.</summary>
        public Guid? ImageId { get; set; }

        /// <summary>Optional. Fetch all images for a given room (by GUID).</summary>
        public Guid? RoomId { get; set; }

        /// <summary>Optional. Fetch all images for the room whose Name equals this value (case-insensitive).</summary>
        public string? RoomName { get; set; }
    }
}
