namespace TelegramYtDlpBot.Persistence;

/// <summary>
/// Manages database persistence for download jobs, processed messages, and application state.
/// </summary>
public interface IStateManager : IDisposable
{
    /// <summary>
    /// Initializes the database schema from schema.sql if tables don't exist.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a download job to the database.
    /// </summary>
    Task SaveJobAsync(Models.DownloadJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the next queued job ordered by CreatedAt (FIFO).
    /// </summary>
    Task<Models.DownloadJob?> GetNextQueuedJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status and related fields of a download job.
    /// </summary>
    Task UpdateJobStatusAsync(Guid jobId, Models.JobStatus status, string? errorMessage = null, string? outputPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the retry count for a failed job.
    /// </summary>
    Task IncrementRetryCountAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about jobs in each status.
    /// </summary>
    Task<Models.QueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a message has already been processed.
    /// </summary>
    Task<bool> IsMessageProcessedAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a message has been processed.
    /// </summary>
    Task SaveProcessedMessageAsync(long messageId, long channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an application state value by key.
    /// </summary>
    Task<string?> GetStateValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an application state value by key.
    /// </summary>
    Task SetStateValueAsync(string key, string value, CancellationToken cancellationToken = default);
}
