using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.RoomImage.Command.UploadRoomImage
{
    /// <summary>
    /// Command for uploading a new image linked to a specific Room.
    /// Bound from a multipart/form-data request.
    /// </summary>
    public class UploadRoomImageCommand : IRequest<UploadRoomImageResponse>
    {
        /// <summary>The Room this image belongs to. Must exist in the database.</summary>
        public Guid RoomId { get; set; }

        /// <summary>The image file sent from the client.</summary>
        public IFormFile? Image { get; set; }
    }
}
