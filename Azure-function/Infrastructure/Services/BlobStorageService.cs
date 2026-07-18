using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Concrete Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
    ///
    /// Configuration in appsettings.json:
    /// <code>
    /// "AzureStorage": {
    ///   "ConnectionString":    "&lt;your-connection-string&gt;",
    ///   "ContainerName":       "documents",
    ///   "SessionVideosFolder": "session-videos"   // optional, defaults to "session-videos"
    /// }
    /// </code>
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        // ── Allowed video extensions ──────────────────────────────────────────
        private static readonly HashSet<string> _allowedVideoExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".mov", ".avi", ".webm" };

        private static readonly Dictionary<string, string> _videoContentTypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { ".mp4",  "video/mp4"       },
                { ".mov",  "video/quicktime" },
                { ".avi",  "video/x-msvideo" },
                { ".webm", "video/webm"      }
            };

        // ── Allowed image extensions ──────────────────────────────────────────
        private static readonly HashSet<string> _allowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        private static readonly Dictionary<string, string> _imageContentTypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { ".jpg",  "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png",  "image/png"  },
                { ".gif",  "image/gif"  },
                { ".bmp",  "image/bmp"  },
                { ".webp", "image/webp" }
            };

        // ── Configuration ─────────────────────────────────────────────────────
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly string _sessionVideosFolder;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(
            IConfiguration configuration,
            ILogger<BlobStorageService> logger)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is missing from configuration.");

            _containerName = configuration["AzureStorage:ContainerName"]
                ?? throw new InvalidOperationException(
                    "AzureStorage:ContainerName is missing from configuration.");

            // Defaults to "session-videos" if the key is absent.
            _sessionVideosFolder = configuration["AzureStorage:SessionVideosFolder"]
                ?? "session-videos";

            _logger = logger;
        }

        // ── Internal helper ───────────────────────────────────────────────────

        private BlobContainerClient GetContainerClient()
        {
            var serviceClient = new BlobServiceClient(_connectionString);
            return serviceClient.GetBlobContainerClient(_containerName);
        }

        // ── Lobby / general video ─────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<string> UploadVideoAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("No file was provided or the file is empty.");

            var extension = Path.GetExtension(file.FileName);

            if (!_allowedVideoExtensions.Contains(extension))
                throw new ArgumentException(
                    $"Unsupported file type '{extension}'. " +
                    $"Allowed: {string.Join(", ", _allowedVideoExtensions)}");

            _logger.LogInformation(
                "[BlobStorage] Uploading lobby video. FileName={FileName} Size={Size} ContentType={ContentType}",
                file.FileName, file.Length, file.ContentType);

            var contentType = _videoContentTypeMap.TryGetValue(extension, out var ct)
                ? ct : "application/octet-stream";

            const string folderName = "lobby_video";
            var blobName = $"{folderName}/{Guid.NewGuid()}{extension}";

            var containerClient = GetContainerClient();
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation(
                "[BlobStorage] Target blob: {BlobName} in container '{Container}'",
                blobName, _containerName);

            try
            {
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[BlobStorage] Upload failed for blob '{BlobName}'", blobName);
                throw new InvalidOperationException(
                    $"Azure Blob upload failed for '{blobName}'. See inner exception for details.", ex);
            }

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation("[BlobStorage] Upload succeeded. BlobUrl={BlobUrl}", blobUrl);
            return blobUrl;
        }

        /// <inheritdoc/>
        public async Task<System.IO.Stream?> GetBlobStreamAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = GetContainerClient();
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync(cancellationToken))
                {
                    _logger.LogWarning("[BlobStorage] Blob not found: {BlobName}", blobName);
                    return null;
                }

                var response = await blobClient.DownloadStreamingAsync(
                    cancellationToken: cancellationToken);
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[BlobStorage] Error downloading blob stream: {BlobName}", blobName);
                throw;
            }
        }

        // ── Room images ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<string> UploadImageAsync(
            IFormFile file,
            string blobPath,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException(
                    "No image file was provided or the file is empty.");

            if (string.IsNullOrWhiteSpace(blobPath))
                throw new ArgumentException(
                    "blobPath must not be null or empty.", nameof(blobPath));

            var extension = Path.GetExtension(file.FileName);

            if (!_allowedImageExtensions.Contains(extension))
                throw new ArgumentException(
                    $"Unsupported image type '{extension}'. " +
                    $"Allowed: {string.Join(", ", _allowedImageExtensions)}");

            _logger.LogInformation(
                "[BlobStorage] Uploading image. BlobPath={BlobPath} FileName={FileName} Size={Size}",
                blobPath, file.FileName, file.Length);

            var contentType = _imageContentTypeMap.TryGetValue(extension, out var ct)
                ? ct : "application/octet-stream";

            var containerClient = GetContainerClient();
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobPath);

            try
            {
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[BlobStorage] Image upload failed for blob '{BlobPath}'", blobPath);
                throw new InvalidOperationException(
                    $"Azure Blob upload failed for '{blobPath}'. See inner exception for details.", ex);
            }

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation(
                "[BlobStorage] Image upload succeeded. BlobUrl={BlobUrl}", blobUrl);
            return blobUrl;
        }

        /// <inheritdoc/>
        public async Task<(Stream Content, string ContentType)?> DownloadImageAsync(
            string blobPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = GetContainerClient();
                var blobClient = containerClient.GetBlobClient(blobPath);

                if (!await blobClient.ExistsAsync(cancellationToken))
                {
                    _logger.LogWarning(
                        "[BlobStorage] Image blob not found: {BlobPath}", blobPath);
                    return null;
                }

                var props = await blobClient.GetPropertiesAsync(
                    cancellationToken: cancellationToken);
                var contentType = props.Value.ContentType ?? "image/png";

                var download = await blobClient.DownloadStreamingAsync(
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[BlobStorage] Image download started. BlobPath={BlobPath} ContentType={ContentType}",
                    blobPath, contentType);

                return (download.Value.Content, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[BlobStorage] Error downloading image blob: {BlobPath}", blobPath);
                throw;
            }
        }

        // ── Session videos ────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Blob path structure:
        /// <code>
        /// {sessionVideosFolder}/{eventId}/{speakerId}/{startTime:yyyyMMdd-HHmmss}_{endTime:yyyyMMdd-HHmmss}/{guid}.{ext}
        /// </code>
        /// Example:
        /// <code>
        /// session-videos/4d8f7d1a-.../8c7a2b3d-.../20260623-100000_20260623-110000/6f9c8b7a-....mp4
        /// </code>
        /// </remarks>
        public async Task<string> UploadSessionVideoAsync(
    IFormFile file,
    Guid speakerId,
    Guid RoomId,
    DateTime startTime,
    DateTime endTime,
    CancellationToken cancellationToken = default)
        {
            // ── 1. Validate ───────────────────────────────────────────────────
            if (file is null || file.Length == 0)
                throw new ArgumentException(
                    "Session video file is null or empty.", nameof(file));

            var extension = Path.GetExtension(file.FileName);

            if (!_allowedVideoExtensions.Contains(extension))
                throw new ArgumentException(
                    $"Unsupported video extension '{extension}'. " +
                    $"Allowed: {string.Join(", ", _allowedVideoExtensions)}");

            // ── 2. Build structured blob path ─────────────────────────────────
            //
            //  session-videos/
            //    {eventId}/
            //      {speakerId}/
            //        {startTime:yyyyMMdd-HHmmss}_{endTime:yyyyMMdd-HHmmss}/
            //          {guid}.{ext}
            //
            var timePart = $"{startTime:yyyyMMdd-HHmmss}_{endTime:yyyyMMdd-HHmmss}";
            var fileName = $"{Guid.NewGuid()}{extension}";
            var blobName = $"{_sessionVideosFolder}/{RoomId}/{fileName}";

            _logger.LogInformation(
    "[BlobStorage] Uploading session video. " +
    "SpeakerId={SpeakerId} BlobName={BlobName} Size={Size}",
    speakerId,
    blobName,
    file.Length);

            // ── 3. Resolve content type ───────────────────────────────────────
            var contentType = _videoContentTypeMap.TryGetValue(extension, out var ct)
                ? ct : "application/octet-stream";

            // ── 4. Upload ─────────────────────────────────────────────────────
            var containerClient = GetContainerClient();
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            try
            {
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[BlobStorage] Session video upload FAILED. BlobName={BlobName}", blobName);

                // Re-throw as InvalidOperationException so the handler can abort
                // session creation and surface a meaningful error to the client.
                throw new InvalidOperationException(
                    $"Failed to upload session video to Azure Blob Storage " +
                    $"(blob: '{blobName}'). Session has NOT been created. " +
                    "See inner exception for details.",
                    ex);
            }

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation(
                "[BlobStorage] Session video upload succeeded. BlobUrl={BlobUrl}", blobUrl);

            return blobUrl;
        }

        public async Task<System.Collections.Generic.List<string>> ListBlobsAsync(
            string prefix,
            CancellationToken cancellationToken = default)
        {
            var containerClient = GetContainerClient();
            var result = new System.Collections.Generic.List<string>();

            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                return result;
            }

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                result.Add(blobItem.Name);
            }

            return result;
        }
    }
}
