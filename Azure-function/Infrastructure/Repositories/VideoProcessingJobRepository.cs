using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VideoProcessingJobRepository : IVideoProcessingJobRepository
{
    private readonly AppDbContext _context;

    public VideoProcessingJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<VideoJob>> GetQueuedJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .Where(j => j.Status == "Queued")
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<VideoJob?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    public async Task MarkProcessingAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _context.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job != null)
        {
            job.Status = "Processing";
            job.ProcessingStartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkReadyAsync(Guid jobId, string manifestUrl, string azureFolderPath, int? durationSeconds, CancellationToken cancellationToken = default)
    {
        var job = await _context.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job != null)
        {
            job.Status = "Ready";
            job.ManifestUrl = manifestUrl;
            job.AzureFolderPath = azureFolderPath;
            job.DurationSeconds = durationSeconds;
            job.ProcessingCompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var job = await _context.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job != null)
        {
            job.Status = "Failed";
            job.ErrorMessage = errorMessage;
            job.ProcessingCompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementRetryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _context.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job != null)
        {
            job.RetryCount += 1;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddJobAsync(VideoJob job, CancellationToken cancellationToken = default)
    {
        await _context.VideoJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetProcessingJobsToQueuedAsync(CancellationToken cancellationToken = default)
    {
        var processingJobs = await _context.VideoJobs
            .Where(j => j.Status == "Processing")
            .ToListAsync(cancellationToken);

        foreach (var job in processingJobs)
        {
            job.Status = "Queued";
            job.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
