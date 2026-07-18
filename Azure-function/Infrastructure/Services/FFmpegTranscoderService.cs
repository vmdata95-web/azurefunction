using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FFmpegTranscoderService : IVideoTranscoderService
{
    private readonly ILogger<FFmpegTranscoderService> _logger;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FFmpegTranscoderService(IConfiguration configuration, ILogger<FFmpegTranscoderService> logger)
    {
        _logger = logger;
        _ffmpegPath = configuration["VideoProcessing:FFmpegPath"] ?? "ffmpeg";
        _ffprobePath = configuration["VideoProcessing:FFprobePath"] ?? "ffprobe";
    }

    public async Task<int?> TranscodeToHlsAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FFmpegTranscoder] Starting transcoding. Input: {Input}, OutputDir: {OutputDir}", inputFilePath, outputDirectory);

        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException("Input video file not found for transcoding", inputFilePath);
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // ── 1. Query duration using ffprobe ───────────────────────────────
        int? durationSeconds = await GetVideoDurationAsync(inputFilePath, cancellationToken);
        _logger.LogInformation("[FFmpegTranscoder] Probed video duration: {Duration} seconds", durationSeconds);

        // ── 2. Run FFmpeg transcode ───────────────────────────────────────
        string playlistPath = Path.Combine(outputDirectory, "index.m3u8");
        // Exact user requested command parameters:
        // -i input.mp4 -c:v libx264 -c:a aac -f hls -hls_time 6 -hls_list_size 50 -hls_flags delete_segments index.m3u8
        string arguments = $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -f hls -hls_time 6 -hls_list_size 50 -hls_flags delete_segments \"{playlistPath}\"";

        _logger.LogInformation("[FFmpegTranscoder] Executing: {Path} {Args}", _ffmpegPath, arguments);

        await RunProcessAsync(_ffmpegPath, arguments, cancellationToken);

        _logger.LogInformation("[FFmpegTranscoder] Transcoding completed successfully. Playlist generated at {Path}", playlistPath);

        return durationSeconds;
    }

    private async Task<int?> GetVideoDurationAsync(string inputFilePath, CancellationToken cancellationToken)
    {
        string arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFilePath}\"";
        _logger.LogInformation("[FFmpegTranscoder] Querying duration with: {Path} {Args}", _ffprobePath, arguments);

        try
        {
            string output = await RunProcessWithOutputAsync(_ffprobePath, arguments, cancellationToken);
            if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDuration))
            {
                return (int)Math.Round(parsedDuration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FFmpegTranscoder] Failed to query video duration using ffprobe. Fallback to parsing ffmpeg stderr is not active. Using null duration.");
        }

        return null;
    }

    private async Task RunProcessAsync(string commandPath, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = commandPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(true);

        process.Start();

        // Capture standard error stream which is where ffmpeg writes most logs
        var errorReaderTask = process.StandardError.ReadToEndAsync();
        var outputReaderTask = process.StandardOutput.ReadToEndAsync();

        // Support cancellation
        using (cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { }
            tcs.TrySetCanceled(cancellationToken);
        }))
        {
            await tcs.Task;
        }

        string errorOutput = await errorReaderTask;
        string standardOutput = await outputReaderTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("[FFmpegTranscoder] Process failed with exit code {ExitCode}.\nStdOut: {StdOut}\nStdErr: {StdErr}", process.ExitCode, standardOutput, errorOutput);
            throw new Exception($"FFmpeg/FFprobe execution failed with exit code {process.ExitCode}. Stderr: {errorOutput}");
        }
    }

    private async Task<string> RunProcessWithOutputAsync(string commandPath, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = commandPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            // Read output on exit
            try
            {
                tcs.TrySetResult(process.StandardOutput.ReadToEnd());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        process.Start();

        var errorReaderTask = process.StandardError.ReadToEndAsync();

        using (cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { }
            tcs.TrySetCanceled(cancellationToken);
        }))
        {
            string output = await tcs.Task;
            string errorOutput = await errorReaderTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("[FFmpegTranscoder] Process failed with exit code {ExitCode}. Stderr: {StdErr}", process.ExitCode, errorOutput);
                throw new Exception($"FFmpeg/FFprobe process failed with exit code {process.ExitCode}. Stderr: {errorOutput}");
            }

            return output;
        }
    }
}
