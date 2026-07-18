using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IVideoTranscoderService
{
    /// <summary>
    /// Transcodes a local MP4 file to HLS (m3u8 + ts segments).
    /// </summary>
    /// <param name="inputFilePath">Absolute path to local MP4 file.</param>
    /// <param name="outputDirectory">Directory to save HLS outputs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the transcode result (e.g. Duration in seconds).</returns>
    Task<int?> TranscodeToHlsAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default);
}
