using System;

namespace Domain.Entities;

public class VideoJob
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? ExhibitorId { get; set; }
    public string RawVideoUrl { get; set; } = string.Empty;
    public string? ManifestUrl { get; set; }
    public string? AzureFolderPath { get; set; }
    public string Status { get; set; } = "Queued"; // Queued, Processing, Ready, Failed
    public int RetryCount { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
