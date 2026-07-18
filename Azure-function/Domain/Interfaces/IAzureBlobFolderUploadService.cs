using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IAzureBlobFolderUploadService
{
    /// <summary>
    /// Uploads all files inside a directory to Azure Blob Storage under a target folder path prefix.
    /// </summary>
    Task UploadFolderAsync(string localDirectoryPath, string blobFolderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a single file to Azure Blob Storage under a target path.
    /// </summary>
    Task<string> UploadFileAsync(string localFilePath, string blobFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from Azure Blob Storage using its URL and writes it to a local file.
    /// </summary>
    Task DownloadFileAsync(string blobUrl, string destinationLocalPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs the full public URL of a blob inside the configured container given its path.
    /// </summary>
    string GetBlobUrl(string blobPath);
}
