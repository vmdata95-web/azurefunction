//using Application.Features.RoomImage.Command.UploadRoomImage;
//using Application.Features.RoomImage.Query.GetRoomImage;
//using Domain.Interfaces;
//using MediatR;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Sat_Kon.Controllers
//{
//    /// <summary>
//    /// REST API controller for Room image upload and retrieval.
//    ///
//    /// Endpoints:
//    ///   POST  /api/roomimage/upload              — upload an image for a room (requires auth)
//    ///   GET   /api/roomimage/{imageId}           — fetch image metadata by ID (public)
//    ///   GET   /api/roomimage/room/{roomId}       — fetch all images for a room by GUID (public)
//    ///   GET   /api/roomimage/room/name/{name}    — fetch all images for a room by name e.g. "Login" (public)
//    ///   GET   /api/roomimage/file/{imageId}      — proxy raw image bytes, no auth needed (public)
//    /// </summary>
//    [ApiController]
//    [Route("api/[controller]")]
//    public class RoomImageController : ControllerBase
//    {
//        private readonly IMediator _mediator;
//        private readonly IBlobStorageService _blobStorageService;
//        private readonly IRoomImageRepository _roomImageRepository;

//        public RoomImageController(
//            IMediator mediator,
//            IBlobStorageService blobStorageService,
//            IRoomImageRepository roomImageRepository)
//        {
//            _mediator             = mediator;
//            _blobStorageService   = blobStorageService;
//            _roomImageRepository  = roomImageRepository;
//        }

//        // ──────────────────────────────────────────────────────────────────────
//        // POST  /api/roomimage/upload
//        // ──────────────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Uploads an image and links it to the specified Room.
//        ///
//        /// Form fields:
//        ///   roomId  — Guid   (required) Room that owns this image
//        ///   image   — file   (required) The image to upload (jpg/jpeg/png/gif/bmp/webp, max 10 MB)
//        ///
//        /// Returns 201 Created with the new image details on success.
//        /// Requires authentication.
//        /// </summary>
//        [HttpPost("upload")]
//        [Authorize(Roles = "SuperAdmi")]
//        [Consumes("multipart/form-data")]
//        [ProducesResponseType(typeof(UploadRoomImageResponse), StatusCodes.Status201Created)]
//        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        public async Task<IActionResult> Upload(
//            [FromForm] UploadRoomImageCommand command,
//            CancellationToken cancellationToken)
//        {
//            var result = await _mediator.Send(command, cancellationToken);

//            return CreatedAtAction(
//                nameof(GetById),
//                new { imageId = result.ImageId },
//                result);
//        }

//        // ──────────────────────────────────────────────────────────────────────
//        // GET  /api/roomimage/{imageId}
//        // ──────────────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Returns metadata (including blobPath/blobUrl) for a single image.
//        /// Public — no authentication required.
//        /// </summary>
//        [HttpGet("{imageId:guid}")]
//        [Authorize(Roles = "SuperAdmi")]
//        //[AllowAnonymous]
//        [ProducesResponseType(typeof(GetRoomImageResponse), StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        public async Task<IActionResult> GetById(
//            Guid imageId,
//            CancellationToken cancellationToken)
//        {
//            var result = await _mediator.Send(
//                new GetRoomImageQuery { ImageId = imageId },
//                cancellationToken);

//            return Ok(result);
//        }

//        // ──────────────────────────────────────────────────────────────────────
//        // GET  /api/roomimage/room/{roomId}
//        // ──────────────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Returns metadata for all images that belong to the specified Room (by GUID),
//        /// ordered by most-recently-uploaded first.
//        /// Public — no authentication required.
//        /// </summary>
//        [HttpGet("room/{roomId:guid}")]
//        [Authorize(Roles = "SuperAdmi")]
//        //[AllowAnonymous]
//        [ProducesResponseType(typeof(GetRoomImageResponse), StatusCodes.Status200OK)]
//        public async Task<IActionResult> GetByRoom(
//            Guid roomId,
//            CancellationToken cancellationToken)
//        {
//            var result = await _mediator.Send(
//                new GetRoomImageQuery { RoomId = roomId },
//                cancellationToken);

//            return Ok(result);
//        }

//        // ──────────────────────────────────────────────────────────────────────
//        // GET  /api/roomimage/room/name/{roomName}
//        // ──────────────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Returns metadata for all images whose Room.Name matches
//        /// <paramref name="roomName"/> (case-insensitive).
//        /// Useful when you know the room's logical name (e.g. "Login") but not its GUID.
//        /// Public — no authentication required.
//        /// </summary>
//        [HttpGet("room/name/{roomName}")]
//        [Authorize(Roles = "SuperAdmi")]
//        //[AllowAnonymous]
//        [ProducesResponseType(typeof(GetRoomImageResponse), StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        public async Task<IActionResult> GetByRoomName(
//            string roomName,
//            CancellationToken cancellationToken)
//        {
//            var result = await _mediator.Send(
//                new GetRoomImageQuery { RoomName = roomName },
//                cancellationToken);

//            return Ok(result);
//        }

//        // ──────────────────────────────────────────────────────────────────────
//        // GET  /api/roomimage/file/{imageId}
//        // ──────────────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Proxies the raw image bytes from Azure Blob Storage through the backend,
//        /// so the browser never needs direct access to the private storage account.
//        /// Public — no authentication required, call as a plain &lt;img src&gt; URL.
//        /// </summary>
//        [HttpGet("file/{imageId:guid}")]
//        [Authorize(Roles = "SuperAdmi")]
//        //[AllowAnonymous]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        public async Task<IActionResult> DownloadImage(
//            Guid imageId,
//            CancellationToken cancellationToken)
//        {
//            // 1. Resolve the blobPath from the database
//            var image = await _roomImageRepository.GetByIdAsync(imageId, cancellationToken);
//            if (image is null)
//                return NotFound($"Image '{imageId}' not found.");

//            // 2. Stream the bytes from Azure using the backend's private connection
//            var result = await _blobStorageService.DownloadImageAsync(image.BlobPath, cancellationToken);
//            if (result is null)
//                return NotFound($"Blob '{image.BlobPath}' not found in storage.");

//            // 3. Cache in browser for 1 hour so repeat visits load instantly
//            Response.Headers["Cache-Control"] = "public, max-age=3600";

//            // 4. Return the raw bytes — browser renders it as an image
//            return File(result.Value.Content, result.Value.ContentType);
//        }
//    }
//}
