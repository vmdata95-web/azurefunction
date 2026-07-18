using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.RoomImage.Command.UploadRoomImage
{
    /// <summary>
    /// Handles the POST /api/roomimage endpoint.
    ///
    /// Flow:
    ///   1. Verify the target Room exists (FK integrity check).
    ///   2. Generate a unique image ID (Guid).
    ///   3. Build the blob path:  {roomId}/{uniqueId}{extension}
    ///   4. Upload the image to Azure Blob Storage via IBlobStorageService.
    ///   5. Persist a RoomImage record in the database.
    ///   6. Return an UploadRoomImageResponse with all details.
    /// </summary>
    public class UploadRoomImageHandler
        : IRequestHandler<UploadRoomImageCommand, UploadRoomImageResponse>
    {
        private readonly IRoomImageRepository _roomImageRepository;
        private readonly IBlobStorageService  _blobStorageService;
        private readonly ILogger<UploadRoomImageHandler> _logger;

        public UploadRoomImageHandler(
            IRoomImageRepository roomImageRepository,
            IBlobStorageService  blobStorageService,
            ILogger<UploadRoomImageHandler> logger)
        {
            _roomImageRepository = roomImageRepository;
            _blobStorageService  = blobStorageService;
            _logger              = logger;
        }

        public async Task<UploadRoomImageResponse> Handle(
            UploadRoomImageCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[UploadRoomImage] Request received. RoomId={RoomId} FileName={FileName}",
                request.RoomId, request.Image?.FileName);

            // ── 1. Verify Room exists ─────────────────────────────────────────
            var roomExists = await _roomImageRepository.RoomExistsAsync(
                request.RoomId, cancellationToken);

            if (!roomExists)
            {
                _logger.LogWarning(
                    "[UploadRoomImage] Room not found. RoomId={RoomId}", request.RoomId);

                throw new KeyNotFoundException(
                    $"Room with Id '{request.RoomId}' does not exist.");
            }

            // ── 2. Generate unique image ID ───────────────────────────────────
            var imageId   = Guid.NewGuid();
            var extension = System.IO.Path.GetExtension(request.Image!.FileName);

            // ── 3. Build blob path:  {roomId}/{uniqueId}{extension} ───────────
            var blobPath = $"{request.RoomId}/{imageId}{extension}";

            _logger.LogInformation(
                "[UploadRoomImage] Uploading to Azure. BlobPath={BlobPath}", blobPath);

            // ── 4. Upload to Azure Blob Storage ───────────────────────────────
            var blobUrl = await _blobStorageService.UploadImageAsync(
                request.Image,
                blobPath,
                cancellationToken);

            _logger.LogInformation(
                "[UploadRoomImage] Upload succeeded. BlobUrl={BlobUrl}", blobUrl);

            // ── 5. Persist RoomImage record ───────────────────────────────────
            var roomImage = new Domain.Entities.RoomImage
            {
                Id               = imageId,
                RoomId           = request.RoomId,
                BlobPath         = blobPath,
                BlobUrl          = blobUrl,
                OriginalFileName = request.Image.FileName,
                ContentType      = request.Image.ContentType,
                CreatedAt        = DateTime.UtcNow
            };

            await _roomImageRepository.AddAsync(roomImage, cancellationToken);
            await _roomImageRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[UploadRoomImage] Image persisted. ImageId={ImageId} RoomId={RoomId}",
                imageId, request.RoomId);

            // ── 6. Return response ────────────────────────────────────────────
            return new UploadRoomImageResponse
            {
                ImageId          = imageId,
                RoomId           = request.RoomId,
                BlobPath         = blobPath,
                BlobUrl          = blobUrl,
                OriginalFileName = request.Image.FileName,
                CreatedAt        = roomImage.CreatedAt
            };
        }
    }
}
