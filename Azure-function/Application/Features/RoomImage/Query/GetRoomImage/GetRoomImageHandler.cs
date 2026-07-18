using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.RoomImage.Query.GetRoomImage
{
    /// <summary>
    /// Handles the GET /api/roomimage endpoint.
    ///
    /// Lookup strategy:
    ///   - If ImageId is provided → fetch that single image record.
    ///   - If only RoomId is provided → fetch all images for that room.
    ///
    /// The handler maps domain entities to <see cref="RoomImageDto"/> objects.
    /// It does NOT stream the blob — callers receive the stored BlobUrl which
    /// can be consumed directly by frontend &lt;img&gt; tags or further processed
    /// by a dedicated streaming endpoint.
    /// </summary>
    public class GetRoomImageHandler
        : IRequestHandler<GetRoomImageQuery, GetRoomImageResponse>
    {
        private readonly IRoomImageRepository _roomImageRepository;
        private readonly ILogger<GetRoomImageHandler> _logger;

        public GetRoomImageHandler(
            IRoomImageRepository roomImageRepository,
            ILogger<GetRoomImageHandler> logger)
        {
            _roomImageRepository = roomImageRepository;
            _logger              = logger;
        }

        public async Task<GetRoomImageResponse> Handle(
            GetRoomImageQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[GetRoomImage] Request received. ImageId={ImageId} RoomId={RoomId} RoomName={RoomName}",
                request.ImageId, request.RoomId, request.RoomName);

            List<Domain.Entities.RoomImage> images;

            // ── Fetch by ImageId (single) ─────────────────────────────────────
            if (request.ImageId.HasValue)
            {
                var image = await _roomImageRepository.GetByIdAsync(
                    request.ImageId.Value, cancellationToken);

                if (image is null)
                {
                    _logger.LogWarning(
                        "[GetRoomImage] Image not found. ImageId={ImageId}", request.ImageId);

                    throw new KeyNotFoundException(
                        $"Image with Id '{request.ImageId}' does not exist.");
                }

                images = new List<Domain.Entities.RoomImage> { image };
            }
            // ── Fetch by Room GUID ────────────────────────────────────────────
            else if (request.RoomId.HasValue)
            {
                images = await _roomImageRepository.GetByRoomIdAsync(
                    request.RoomId.Value, cancellationToken);

                _logger.LogInformation(
                    "[GetRoomImage] Found {Count} image(s) for RoomId={RoomId}",
                    images.Count, request.RoomId);
            }
            // ── Fetch by Room Name ────────────────────────────────────────────
            else
            {
                images = await _roomImageRepository.GetByRoomNameAsync(
                    request.RoomName!, cancellationToken);

                _logger.LogInformation(
                    "[GetRoomImage] Found {Count} image(s) for RoomName={RoomName}",
                    images.Count, request.RoomName);
            }

            // ── Map to DTOs ───────────────────────────────────────────────────
            var dtos = images.Select(img => new RoomImageDto
            {
                ImageId          = img.Id,
                RoomId           = img.RoomId,
                BlobPath         = img.BlobPath,
                BlobUrl          = img.BlobUrl,
                OriginalFileName = img.OriginalFileName,
                ContentType      = img.ContentType,
                CreatedAt        = img.CreatedAt
            }).ToList();

            return new GetRoomImageResponse { Images = dtos };
        }
    }
}
