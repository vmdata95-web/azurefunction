
using Application.Common.Exceptions;
using Application.Features.CreateSession.Command.CreateSession;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CreateSession.Command.CreateSession
{
    /// <summary>
    /// Handles the <see cref="CreateSessionCommand"/> request.
    ///
    /// Workflow:
    ///   1. Validate room exists.
    ///   2. Validate speaker exists.
    ///   3. Validate the requested time slot is free.
    ///   4. If a video file is provided, upload it to Azure Blob Storage under the
    ///      structured path:
    ///      <c>session-videos/{eventId}/{speakerId}/{startTime:yyyyMMdd-HHmmss}_{endTime:yyyyMMdd-HHmmss}/{guid}.{ext}</c>
    ///      If the upload fails, <see cref="InvalidOperationException"/> is thrown and
    ///      the session is NOT created — no partial record is persisted.
    ///   5. Persist the <see cref="Session"/> entity with the Blob URL stored in
    ///      <c>Session.VideoUrl</c>.
    /// </summary>
    public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, Guid>
    {
        private readonly ISessionRepository _repository;
        private readonly IBlobStorageService _blobStorage;
        private readonly ILogger<CreateSessionCommandHandler> _logger;

        public CreateSessionCommandHandler(
            ISessionRepository repository,
            IBlobStorageService blobStorage,
            ILogger<CreateSessionCommandHandler> logger)
        {
            _repository = repository;
            _blobStorage = blobStorage;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateSessionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
    "[CreateSession] Starting. RoomId={RoomId} SpeakerId={SpeakerId} Title={Title}",
    request.RoomId,
    request.SpeakerId,
    request.Title);

            // ── 1. Validate room ──────────────────────────────────────────────
            if (!await _repository.RoomExistsAsync(request.RoomId))
            {
                _logger.LogWarning(
                    "[CreateSession] Room not found. RoomId={RoomId}", request.RoomId);
                throw new BadRequestException("Room not found");
            }

            // ── 2. Validate speaker ───────────────────────────────────────────
            if (!await _repository.SpeakerExistsAsync(request.SpeakerId))
            {
                _logger.LogWarning(
                    "[CreateSession] Speaker not found. SpeakerId={SpeakerId}",
                    request.SpeakerId);
                throw new BadRequestException("Speaker not found");
            }

            // ── 3. Validate time slot ─────────────────────────────────────────
            var available = await _repository.IsTimeSlotAvailable(
                request.RoomId,
                request.StartTime,
                request.EndTime);

            if (!available)
            {
                _logger.LogWarning(
                    "[CreateSession] Time slot already booked. " +
                    "RoomId={RoomId} Start={Start} End={End}",
                    request.RoomId, request.StartTime, request.EndTime);
                throw new BadRequestException("Time slot already booked");
            }

  
            //
            string videoUrl = string.Empty;

            if (request.Video is not null)
            {
                _logger.LogInformation(
                    "[CreateSession] Video file detected. FileName={FileName} Size={Size}. " +
                    "Uploading to Azure Blob Storage…",
                    request.Video.FileName, request.Video.Length);

                // InvalidOperationException propagates to the caller if upload fails;
                // session creation is aborted — no partial record is saved.
                videoUrl = await _blobStorage.UploadSessionVideoAsync(
                    file: request.Video,
                    RoomId: request.RoomId,
                    speakerId: request.SpeakerId,
                    startTime: request.StartTime,
                    endTime: request.EndTime,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[CreateSession] Video uploaded successfully. BlobUrl={BlobUrl}", videoUrl);
            }

            // ── 5. Persist session ────────────────────────────────────────────
            var session = new Session
            {
                Id = Guid.NewGuid(),
                RoomId = request.RoomId,
                SpeakerId = request.SpeakerId,
                Title = request.Title,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                VideoUrl = videoUrl,          // Blob URL (empty string if no video)
                Status = request.Status
            };

            var sessionId = await _repository.CreateSessionAsync(session);

            _logger.LogInformation(
                "[CreateSession] Session created successfully. SessionId={SessionId} VideoUrl={VideoUrl}",
                sessionId, videoUrl);

            return sessionId;
        }
    }
}