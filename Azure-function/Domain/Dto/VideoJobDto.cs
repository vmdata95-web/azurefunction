using System;

namespace Domain.Dto;

public class VideoJobDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? ExhibitorId { get; set; }
    public string RawVideoUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
