using TelegramYtDlpBot.Models;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Manages the download job queue with SQLite persistence.
/// </summary>
public interface IDownloadQueue
{
    /// <summary>
    /// Add a new download job to the queue.
    /// </summary>
    /// <param name="messageId">Source message ID</param>
    /// <param name="url">URL to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created job ID</returns>
    Task<Guid> EnqueueAsync(long messageId, string url, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get the next queued job (FIFO).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next job or null if queue is empty</returns>
    Task<DownloadJob?> DequeueAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as in progress.
    /// </summary>
    Task MarkInProgressAsync(Guid jobId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as completed.
    /// </summary>
    Task MarkCompletedAsync(Guid jobId, string outputPath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as failed.
    /// </summary>
    Task MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retry a failed job (increment retry count and re-queue).
    /// </summary>
    /// <returns>True if job was retried, false if max retries reached</returns>
    Task<bool> RetryJobAsync(Guid jobId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get queue statistics.
    /// </summary>
    Task<QueueStats> GetStatsAsync(CancellationToken cancellationToken);
}
