namespace TelegramYtDlpBot.Models;

/// <summary>
/// Database entity representing a download job in the queue.
/// </summary>
public class DownloadJob
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public long MessageId { get; init; }
    public string Url { get; init; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputPath { get; set; }
    public int RetryCount { get; set; } = 0;
}

public enum JobStatus
{
    Queued,
    InProgress,
    Completed,
    Failed
}

public class QueueStats
{
    public int QueuedCount { get; init; }
    public int InProgressCount { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
}
