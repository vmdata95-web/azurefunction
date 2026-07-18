using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AzureBlobFolderUploadService : IAzureBlobFolderUploadService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobFolderUploadService> _logger;

    public AzureBlobFolderUploadService(IConfiguration configuration, ILogger<AzureBlobFolderUploadService> logger)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is missing from configuration.");

        _containerName = configuration["AzureStorage:ContainerName"]
            ?? throw new InvalidOperationException("AzureStorage:ContainerName is missing from configuration.");

        _logger = logger;
    }

    public async Task UploadFolderAsync(string localDirectoryPath, string blobFolderPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AzureBlobFolderUpload] Starting upload of directory {LocalDir} to blob folder {BlobFolder}", localDirectoryPath, blobFolderPath);

        if (!Directory.Exists(localDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Local directory '{localDirectoryPath}' does not exist.");
        }

        var files = Directory.GetFiles(localDirectoryPath, "*", SearchOption.AllDirectories);
        _logger.LogInformation("[AzureBlobFolderUpload] Found {Count} files to upload.", files.Length);

        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(localDirectoryPath, file).Replace("\\", "/");
            var targetBlobPath = $"{blobFolderPath.TrimEnd('/')}/{relativePath}";

            _logger.LogInformation("[AzureBlobFolderUpload] Uploading file {LocalFile} -> {BlobPath}", file, targetBlobPath);
            await UploadFileInternalAsync(containerClient, file, targetBlobPath, cancellationToken);
        }

        _logger.LogInformation("[AzureBlobFolderUpload] Completed uploading directory {LocalDir}", localDirectoryPath);
    }

    public async Task<string> UploadFileAsync(string localFilePath, string blobFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AzureBlobFolderUpload] Starting upload of single file {LocalFile} to {BlobPath}", localFilePath, blobFilePath);

        if (!File.Exists(localFilePath))
        {
            throw new FileNotFoundException($"Local file '{localFilePath}' does not exist.", localFilePath);
        }

        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = await UploadFileInternalAsync(containerClient, localFilePath, blobFilePath, cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DownloadFileAsync(string blobUrl, string destinationLocalPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AzureBlobFolderUpload] Downloading blob {BlobUrl} -> {LocalPath}", blobUrl, destinationLocalPath);

        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_containerName);

        // Extract blob name from URL
        var blobName = ExtractBlobNameFromUrl(blobUrl);
        var blobClient = containerClient.GetBlobClient(blobName);

        var directory = Path.GetDirectoryName(destinationLocalPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Open local file stream and download to it
        using var fileStream = new FileStream(destinationLocalPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await blobClient.DownloadToAsync(fileStream, cancellationToken);
        _logger.LogInformation("[AzureBlobFolderUpload] Successfully downloaded blob to {LocalPath}", destinationLocalPath);
    }

    private async Task<BlobClient> UploadFileInternalAsync(BlobContainerClient containerClient, string localFilePath, string blobPath, CancellationToken cancellationToken)
    {
        var blobClient = containerClient.GetBlobClient(blobPath);
        var extension = Path.GetExtension(localFilePath).ToLowerInvariant();

        var contentType = extension switch
        {
            ".mp4" => "video/mp4",
            ".m3u8" => "application/x-mpegURL",
            ".ts" => "video/mp2t",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        using var fileStream = File.OpenRead(localFilePath);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);
        return blobClient;
    }

    private string ExtractBlobNameFromUrl(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            throw new ArgumentException("Blob URL cannot be null or empty.", nameof(blobUrl));
        }

        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid absolute URL format.", nameof(blobUrl));
        }

        // Standard blob url: https://<account>.blob.core.windows.net/<container>/<blob-name>
        // Let's extract everything after container
        var segments = uri.Segments;
        if (segments.Length < 3)
        {
            // Fallback: if URL path is simpler, try to extract last segment or path after host
            return uri.AbsolutePath.TrimStart('/');
        }

        // Skip host (segments[0] is '/' or host segment depending on scheme, segments[1] is container/)
        // All remaining segments are parts of the blob name
        var blobName = string.Join("", segments.Skip(2));
        
        // Decode URL escape sequences like %20 to spaces
        return Uri.UnescapeDataString(blobName);
    }

    public string GetBlobUrl(string blobPath)
    {
        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_containerName);
        return containerClient.GetBlobClient(blobPath).Uri.ToString();
    }
}
