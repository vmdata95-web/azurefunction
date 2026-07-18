using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;

namespace Application.Features.CreateExhibitor.Command.Lobby_video
{
    /// <summary>
    /// Handles the POST /Lobby_video endpoint.
    ///
    /// Flow:
    ///   1. Validates the uploaded file (non-null, supported extension).
    ///   2. Delegates the actual upload to IBlobStorageService, which:
    ///        - Creates the Azure container if it does not exist.
    ///        - Generates a unique GUID-based blob name.
    ///        - Sets the correct Content-Type header.
    ///        - Returns the public Blob URL.
    ///   3. Persists the Blob URL (not a local file path) in the SQL Server database.
    ///
    /// Local file-system dependencies (Directory.CreateDirectory, FileStream, etc.)
    /// have been completely removed.
    /// </summary>
    public class CreateExhibitorHandler
        : IRequestHandler<CreateExhibitorCommand, Guid>
    {
        private readonly IExhibitorRepository _repository;
        private readonly IVideoJobRepository _videoJobRepository;
        private readonly IBlobStorageService   _blobStorageService;
        private readonly ILogger<CreateExhibitorHandler> _logger;

        public CreateExhibitorHandler(
            IExhibitorRepository repository,
            IVideoJobRepository videoJobRepository,
            IBlobStorageService  blobStorageService,
            ILogger<CreateExhibitorHandler> logger)
        {
            _repository         = repository;
            _videoJobRepository = videoJobRepository;
            _blobStorageService = blobStorageService;
            _logger             = logger;
        }

        public async Task<Guid> Handle(
            CreateExhibitorCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[CreateExhibitor] Request received. EventId={EventId}",
                request.EventId);

            // ── 1. Validate file presence ─────────────────────────────────────
            if (request.Logo is null || request.Logo.Length == 0)
            {
                _logger.LogWarning(
                    "[CreateExhibitor] Rejected — no file was provided.");
                throw new BadRequestException("A video file is required.");
            }

            // Check if a video already exists
            if (await _repository.HasExhibitorAsync())
            {
                throw new BadRequestException("A video already exists. Only one video is allowed.");
            }

            // ── 2. Upload to Azure Blob Storage ───────────────────────────────
            // IBlobStorageService validates the extension, creates the container,
            // generates a GUID blob name, and returns the full public Blob URL.
            // Any upload failure or invalid type will surface as an exception here.
            _logger.LogInformation(
                "[CreateExhibitor] Uploading video to Azure Blob Storage. " +
                "FileName={FileName} Size={Size}",
                request.Logo.FileName, request.Logo.Length);

            var blobUrl = await _blobStorageService.UploadVideoAsync(
                request.Logo,
                cancellationToken);

            _logger.LogInformation("Video uploaded successfully. BlobUrl={BlobUrl}", blobUrl);

            // ── 3. Persist exhibitor with Blob URL ────────────────────────────
            // Name  → Blob URL (was local path like /videos/file.mp4)
            // Logo  → Blob URL (mirrors Name; both columns store the URL)
            var exhibitor = new Exhibitor
            {
                Id          = Guid.NewGuid(),
                EventId     = request.EventId,
                Name        = blobUrl,
                Description = request.Description,
                Website     = request.Website,
                url        = blobUrl
            };



            await _repository.AddAsync(exhibitor);

            // ── 4. Create and persist VideoJob record ─────────────────────────
            var videoJob = new VideoJob
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                ExhibitorId = exhibitor.Id,
                RawVideoUrl = blobUrl,
                Status = "Queued",
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
                SessionId = null,
                ManifestUrl = null,
                AzureFolderPath = null,
                DurationSeconds = null,
                ProcessingStartedAt = null,
                ProcessingCompletedAt = null,
                ErrorMessage = null,
                UpdatedAt = null
            };

            await _videoJobRepository.AddAsync(videoJob);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "VideoJob created. Id={VideoJobId}, EventId={EventId}, RawVideoUrl={RawVideoUrl}",
                videoJob.Id, videoJob.EventId, videoJob.RawVideoUrl);

            _logger.LogInformation(
                "[CreateExhibitor] Exhibitor persisted. ExhibitorId={ExhibitorId} BlobUrl={BlobUrl}",
                exhibitor.Id, blobUrl);

            return exhibitor.Id;
        }
    }
}
