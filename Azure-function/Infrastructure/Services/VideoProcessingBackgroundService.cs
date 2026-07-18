using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class VideoProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<VideoProcessingBackgroundService> _logger;

    public VideoProcessingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<VideoProcessingBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[VideoProcessingBackgroundService] Background service starting up...");

        try
        {
            // ── Clean up any leftover 'Processing' status jobs from prior crash/shutdown ──
            _logger.LogInformation("[VideoProcessingBackgroundService] Resetting leftover 'Processing' jobs to 'Queued'...");
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IVideoProcessingJobRepository>();
                await repo.ResetProcessingJobsToQueuedAsync(stoppingToken);
            }
            _logger.LogInformation("[VideoProcessingBackgroundService] Cleanup completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VideoProcessingBackgroundService] Failed to reset leftover processing jobs on startup.");
        }

        // Loop until cancellation requested
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VideoProcessingBackgroundService] Critical error in processing loop.");
            }

            // Poll every 10 seconds (in production, this can be configured)
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, ignore
                break;
            }
        }

        _logger.LogInformation("[VideoProcessingBackgroundService] Background service is stopping.");
    }

    private async Task ProcessQueuedJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IVideoProcessingJobRepository>();

        var jobs = await repo.GetQueuedJobsAsync(stoppingToken);
        if (jobs == null || !jobs.Any())
        {
            return;
        }

        _logger.LogInformation("[VideoProcessingBackgroundService] Found {Count} queued video jobs to process.", jobs.Count);

        foreach (var job in jobs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessJobAsync(job.Id, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VideoProcessingBackgroundService] Unexpected error processing job {JobId}.", job.Id);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Generate a isolated directory path for processing this video
        var tempDir = Path.Combine(Path.GetTempPath(), "VideoProcessing", jobId.ToString());
        var localOriginalMp4Path = Path.Combine(tempDir, "original.mp4");
        var localHlsOutputDir = Path.Combine(tempDir, "hls");

        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IVideoProcessingJobRepository>();
        var uploadService = scope.ServiceProvider.GetRequiredService<IAzureBlobFolderUploadService>();
        var transcoderService = scope.ServiceProvider.GetRequiredService<IVideoTranscoderService>();

        // Fetch fresh job details inside the scope
        var job = await repo.GetJobByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.LogWarning("[VideoProcessingBackgroundService] Job {JobId} not found in database. Skipping.", jobId);
            return;
        }

        if (job.Status != "Queued")
        {
            _logger.LogWarning("[VideoProcessingBackgroundService] Job {JobId} has status '{Status}', expected 'Queued'. Skipping.", jobId, job.Status);
            return;
        }

        _logger.LogInformation("[VideoProcessingBackgroundService] Transitioning Job {JobId} to 'Processing'.", jobId);
        await repo.MarkProcessingAsync(jobId, cancellationToken);

        try
        {
            // Create target folders
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(localHlsOutputDir);

            // Step 1: Download original MP4 from Azure Blob Storage
            _logger.LogInformation("[VideoProcessingBackgroundService] Downloading original video. JobId={JobId}, Url={Url} -> Local={Local}",
                jobId, job.RawVideoUrl, localOriginalMp4Path);

            await uploadService.DownloadFileAsync(job.RawVideoUrl, localOriginalMp4Path, cancellationToken);

            // Step 2: Convert original MP4 to HLS format
            _logger.LogInformation("[VideoProcessingBackgroundService] Transcoding video using FFmpeg. JobId={JobId}", jobId);

            var durationSeconds = await transcoderService.TranscodeToHlsAsync(localOriginalMp4Path, localHlsOutputDir, cancellationToken);

            // Step 3: Define blob destination folder
            string liveVideoBlobFolder = $"live-videos/{job.EventId}";

            // Step 4: Upload generated HLS files (index.m3u8 + segments)
            _logger.LogInformation("[VideoProcessingBackgroundService] Uploading HLS directory contents to destination: {Folder}", liveVideoBlobFolder);
            await uploadService.UploadFolderAsync(localHlsOutputDir, liveVideoBlobFolder, cancellationToken);

            // Step 5: Construct return parameters and mark ready
            string manifestBlobPath = $"{liveVideoBlobFolder}/index.m3u8";
            string manifestUrl = uploadService.GetBlobUrl(manifestBlobPath);

            _logger.LogInformation("[VideoProcessingBackgroundService] Marking Job {JobId} as Ready. ManifestUrl={ManifestUrl}, AzureFolderPath={Folder}, Duration={Duration}s",
                jobId, manifestUrl, liveVideoBlobFolder, durationSeconds);

            await repo.MarkReadyAsync(jobId, manifestUrl, liveVideoBlobFolder, durationSeconds, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[VideoProcessingBackgroundService] Job {JobId} was cancelled due to host shutdown. Re-queueing job.", jobId);
            try
            {
                // Reset job back to Queued since it was cancelled by shutdown rather than an error
                using var resetScope = _serviceScopeFactory.CreateScope();
                var resetRepo = resetScope.ServiceProvider.GetRequiredService<IVideoProcessingJobRepository>();
                var resetJob = await resetRepo.GetJobByIdAsync(jobId, CancellationToken.None);
                if (resetJob != null)
                {
                    resetJob.Status = "Queued";
                    resetJob.UpdatedAt = DateTime.UtcNow;
                    var dbContext = resetScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VideoProcessingBackgroundService] Failed to reset job {JobId} status during cancel shutdown.", jobId);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VideoProcessingBackgroundService] Failed to process job {JobId}.", jobId);

            try
            {
                // In case of error: Mark failed, save message, and increment retry
                // Use CancellationToken.None here so this db operation completes even during shutdown/cancellation
                using var failScope = _serviceScopeFactory.CreateScope();
                var failRepo = failScope.ServiceProvider.GetRequiredService<IVideoProcessingJobRepository>();

                _logger.LogInformation("[VideoProcessingBackgroundService] Registering failure for Job {JobId}.", jobId);
                await failRepo.MarkFailedAsync(jobId, ex.Message, CancellationToken.None);
                await failRepo.IncrementRetryAsync(jobId, CancellationToken.None);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "[VideoProcessingBackgroundService] Failed to update fail state in database for Job {JobId}.", jobId);
            }
        }
        finally
        {
            // Step 7: Clean up temporary files
            try
            {
                if (Directory.Exists(tempDir))
                {
                    _logger.LogInformation("[VideoProcessingBackgroundService] Cleaning up temporary directory {LocalDir}", tempDir);
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[VideoProcessingBackgroundService] Failed to clean up temporary directory {LocalDir}", tempDir);
            }
        }
    }
}