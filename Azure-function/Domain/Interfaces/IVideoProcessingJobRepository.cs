using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IVideoProcessingJobRepository
{
    Task<List<VideoJob>> GetQueuedJobsAsync(CancellationToken cancellationToken = default);
    Task<VideoJob?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task MarkReadyAsync(Guid jobId, string manifestUrl, string azureFolderPath, int? durationSeconds, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task ResetProcessingJobsToQueuedAsync(CancellationToken cancellationToken = default);
    Task AddJobAsync(VideoJob job, CancellationToken cancellationToken = default);
}
