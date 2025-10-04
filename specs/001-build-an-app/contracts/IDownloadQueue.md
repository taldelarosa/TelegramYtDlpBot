# Service Contract: IDownloadQueue

**Purpose**: Manage sequential download job queue with persistence and state tracking.

**Responsibilities**:
- Enqueue URLs as download jobs
- Retrieve next job for processing (FIFO order)
- Update job status and outcomes
- Persist queue state to SQLite for restart recovery
- Track job retry attempts

---

## Interface Definition

```csharp
namespace TelegramYtDlpBot.Services;

/// <summary>
/// Manages the download job queue with SQLite persistence.
/// </summary>
public interface IDownloadQueue
{
    /// <summary>
    /// Add a new download job to the queue.
    /// </summary>
    /// <param name="messageId">Source Telegram message ID</param>
    /// <param name="url">URL to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created job ID</returns>
    Task<Guid> EnqueueAsync(long messageId, string url, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get the next queued job (FIFO order).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next job or null if queue is empty</returns>
    Task<DownloadJob?> DequeueAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as in-progress.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkInProgressAsync(Guid jobId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as completed successfully.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="outputPath">Path to downloaded file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkCompletedAsync(Guid jobId, string outputPath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Mark a job as failed.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="errorMessage">Error details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken);
    
    /// <summary>
    /// Increment retry count for a failed job and re-queue if retries remain.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job was re-queued, false if max retries reached</returns>
    Task<bool> RetryJobAsync(Guid jobId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get current queue statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue statistics</returns>
    Task<QueueStats> GetStatsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Queue statistics for monitoring.
/// </summary>
public record QueueStats(
    int QueuedCount,
    int InProgressCount,
    int CompletedCount,
    int FailedCount,
    DateTime? OldestQueuedJobCreatedAt
);
```

---

## Behavior Specifications

### EnqueueAsync

**Preconditions**:
- Message ID is positive
- URL is valid HTTP/HTTPS (validated by caller)

**Behavior**:
1. Generate new Guid for JobId
2. Insert DownloadJob record into SQLite:
   - JobId, MessageId, Url
   - Status = Queued
   - CreatedAt = UtcNow
   - RetryCount = 0
3. Return JobId

**Postconditions**:
- Job persisted to database
- Job available via `DequeueAsync`

**Error Handling**:
- SQLite write error: Log error, throw InvalidOperationException
- Duplicate URL for same message: Allow (user may repost intentionally)

---

### DequeueAsync

**Preconditions**:
- None (queue may be empty)

**Behavior**:
1. Query SQLite: `SELECT * FROM DownloadJobs WHERE Status = 'Queued' ORDER BY CreatedAt LIMIT 1`
2. If result found: Deserialize to DownloadJob, return
3. If no result: Return null

**Postconditions**:
- Job NOT removed from database (status still Queued)
- Caller must call `MarkInProgressAsync` to update status

**Error Handling**:
- SQLite read error: Log error, throw InvalidOperationException

---

### MarkInProgressAsync

**Preconditions**:
- Job exists in database
- Job status is Queued

**Behavior**:
1. Update job: `SET Status = 'InProgress' WHERE JobId = @JobId AND Status = 'Queued'`
2. Verify 1 row affected (optimistic concurrency check)

**Postconditions**:
- Job status updated to InProgress
- No other concurrent worker can dequeue this job

**Error Handling**:
- Job not found: Log warning, throw InvalidOperationException
- Status not Queued: Log warning, throw InvalidOperationException ("Job already processed")
- SQLite write error: Log error, throw InvalidOperationException

---

### MarkCompletedAsync

**Preconditions**:
- Job exists in database
- Job status is InProgress
- Output path is not empty

**Behavior**:
1. Update job: `SET Status = 'Completed', CompletedAt = @Now, OutputPath = @Path WHERE JobId = @JobId`
2. Verify 1 row affected

**Postconditions**:
- Job status updated to Completed
- CompletedAt and OutputPath set

**Error Handling**:
- Job not found: Log warning, throw InvalidOperationException
- Empty output path: Log error, throw ArgumentException

---

### MarkFailedAsync

**Preconditions**:
- Job exists in database
- Job status is InProgress
- Error message is not empty

**Behavior**:
1. Update job: `SET Status = 'Failed', CompletedAt = @Now, ErrorMessage = @Error WHERE JobId = @JobId`
2. Verify 1 row affected

**Postconditions**:
- Job status updated to Failed
- CompletedAt and ErrorMessage set

**Error Handling**:
- Job not found: Log warning, throw InvalidOperationException
- Empty error message: Log error, throw ArgumentException

---

### RetryJobAsync

**Preconditions**:
- Job exists in database
- Job status is Failed
- RetryCount < 3

**Behavior**:
1. Increment RetryCount: `UPDATE DownloadJobs SET RetryCount = RetryCount + 1 WHERE JobId = @JobId`
2. If RetryCount ≤ 3: Update Status = 'Queued', return true
3. If RetryCount > 3: Leave Status = 'Failed', return false

**Postconditions**:
- If true: Job re-queued for retry
- If false: Job remains failed (max retries exceeded)

**Error Handling**:
- Job not found: Log warning, throw InvalidOperationException
- Job not in Failed status: Log warning, return false

---

### GetStatsAsync

**Preconditions**:
- None

**Behavior**:
1. Query SQLite:
   ```sql
   SELECT 
     COUNT(CASE WHEN Status = 'Queued' THEN 1 END) AS QueuedCount,
     COUNT(CASE WHEN Status = 'InProgress' THEN 1 END) AS InProgressCount,
     COUNT(CASE WHEN Status = 'Completed' THEN 1 END) AS CompletedCount,
     COUNT(CASE WHEN Status = 'Failed' THEN 1 END) AS FailedCount,
     MIN(CASE WHEN Status = 'Queued' THEN CreatedAt END) AS OldestQueuedJobCreatedAt
   FROM DownloadJobs
   ```
2. Return QueueStats record

**Postconditions**:
- Accurate snapshot of queue state

**Error Handling**:
- SQLite read error: Log error, throw InvalidOperationException

---

## Configuration Requirements

**From BotConfiguration.Storage**:
- `DatabasePath` (string): Path to SQLite database file

---

## Dependencies

- `Microsoft.Data.Sqlite` (ADO.NET SQLite provider)
- `ILogger<DownloadQueue>`
- `IOptions<BotConfiguration>`

---

## Testing Strategy

### Unit Tests

**Mocking**: Use in-memory SQLite (`:memory:`) for isolation

**Test Cases**:
- EnqueueAsync_WithValidJob_InsertsToDatabase
- DequeueAsync_WithEmptyQueue_ReturnsNull
- DequeueAsync_WithQueuedJobs_ReturnsFIFO
- MarkInProgressAsync_WithQueuedJob_UpdatesStatus
- MarkCompletedAsync_WithInProgressJob_SetsOutputPath
- MarkFailedAsync_WithInProgressJob_SetsErrorMessage
- RetryJobAsync_WithFailedJob_RequeuesIfRetriesRemain
- RetryJobAsync_WithMaxRetries_ReturnsFalse
- GetStatsAsync_WithMixedJobs_ReturnsAccurateCounts

### Integration Tests

**Persistent Database**: Use temp file database for restart simulation

**Test Cases**:
- EnqueueBeforeRestart_DequeueAfterRestart_JobPersisted
- ConcurrentDequeue_WithMultipleWorkers_NoRaceCondition (unlikely with sequential, but test WAL mode)

---

## Performance Requirements

- **Latency**: Enqueue operation <10ms
- **Latency**: Dequeue operation <5ms (indexed query)
- **Memory**: <10MB for queue management overhead
- **Throughput**: Handle 100+ jobs without degradation

---

## Concurrency Considerations

**Sequential Processing**: Only one worker dequeues at a time (application-level coordination)

**SQLite WAL Mode**: Enables concurrent reads during writes

**Optimistic Concurrency**: `MarkInProgressAsync` uses `WHERE Status = 'Queued'` to prevent double-processing

---

## Example Usage

```csharp
public class DownloadWorker
{
    private readonly IDownloadQueue _queue;
    private readonly IYtDlpExecutor _executor;
    
    public async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var job = await _queue.DequeueAsync(cancellationToken);
            
            if (job == null)
            {
                await Task.Delay(5000, cancellationToken); // Poll every 5 seconds
                continue;
            }
            
            await _queue.MarkInProgressAsync(job.JobId, cancellationToken);
            
            try
            {
                var outputPath = await _executor.DownloadAsync(job.Url, cancellationToken);
                await _queue.MarkCompletedAsync(job.JobId, outputPath, cancellationToken);
            }
            catch (Exception ex)
            {
                await _queue.MarkFailedAsync(job.JobId, ex.Message, cancellationToken);
                await _queue.RetryJobAsync(job.JobId, cancellationToken);
            }
        }
    }
}
```

---

**Contract Ownership**: DownloadQueue service implementation  
**Review Status**: Draft - pending implementation
