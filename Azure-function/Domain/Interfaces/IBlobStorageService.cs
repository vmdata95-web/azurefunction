using Microsoft.AspNetCore.Http;

namespace Domain.Interfaces
{
    /// <summary>
    /// Abstracts all Azure Blob Storage operations so that Application handlers
    /// remain free of any cloud-SDK or infrastructure dependencies.
    /// </summary>
    public interface IBlobStorageService
    {
        // ── Lobby / general video ──────────────────────────────────────────────

        /// <summary>
        /// Uploads a video to the configured container under the <c>lobby_video</c> folder
        /// and returns the full public Blob URL.
        ///
        /// Supported video extensions: .mp4, .mov, .avi, .webm
        /// </summary>
        Task<string> UploadVideoAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);

        Task<System.IO.Stream?> GetBlobStreamAsync(
            string blobName,
            CancellationToken cancellationToken = default);

        // ── Room images ────────────────────────────────────────────────────────

        /// <summary>
        /// Uploads an image to the container under the caller-supplied
        /// <paramref name="blobPath"/> and returns the full public Blob URL.
        ///
        /// Supported image extensions: .jpg, .jpeg, .png, .gif, .bmp, .webp
        /// </summary>
        Task<string> UploadImageAsync(
            IFormFile file,
            string blobPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads an image blob and returns (stream, content-type),
        /// or <c>null</c> if the blob does not exist.
        /// </summary>
        Task<(Stream Content, string ContentType)?> DownloadImageAsync(
            string blobPath,
            CancellationToken cancellationToken = default);

        // ── Session videos ─────────────────────────────────────────────────────

        /// <summary>
        /// Uploads a session video to Azure Blob Storage under the structured path:
        /// <c>session-videos/{eventId}/{speakerId}/{startTime:yyyyMMdd-HHmmss}_{endTime:yyyyMMdd-HHmmss}/{guid}.{ext}</c>
        ///
        /// Behaviour:
        ///   - Creates the container if it does not already exist.
        ///   - Generates a GUID-based unique file name while preserving the original extension.
        ///   - Sets the correct Content-Type header on the blob.
        ///   - On failure, throws <see cref="InvalidOperationException"/> so the caller
        ///     can abort session creation without persisting a partial record.
        ///
        /// Supported video extensions: .mp4, .mov, .avi, .webm
        /// </summary>
        /// <param name="file">The video file from the HTTP multipart request.</param>
        /// <param name="eventId">Event the session belongs to — used in the blob path.</param>
        /// <param name="speakerId">Speaker for this session — used in the blob path.</param>
        /// <param name="startTime">Session start time, formatted as <c>yyyyMMdd-HHmmss</c> in the path.</param>
        /// <param name="endTime">Session end time, formatted as <c>yyyyMMdd-HHmmss</c> in the path.</param>
        /// <param name="cancellationToken">Propagates cancellation from the caller.</param>
        /// <returns>The full public Blob URL of the uploaded video.</returns>
        /// <exception cref="ArgumentException">File is null/empty or has an unsupported extension.</exception>
        /// <exception cref="InvalidOperationException">Azure Blob upload failed.</exception>
        Task<string> UploadSessionVideoAsync(
            IFormFile file,
            Guid speakerId,
            Guid RoomId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);

        Task<System.Collections.Generic.List<string>> ListBlobsAsync(
            string prefix,
            CancellationToken cancellationToken = default);
    }
}

