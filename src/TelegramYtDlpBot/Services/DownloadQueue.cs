using TelegramYtDlpBot.Models;

namespace TelegramYtDlpBot.Services;

public class DownloadQueue : IDownloadQueue
{
    public Task<Guid> EnqueueAsync(long messageId, string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<DownloadJob?> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task MarkInProgressAsync(Guid jobId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task MarkCompletedAsync(Guid jobId, string outputPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RetryJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<QueueStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
