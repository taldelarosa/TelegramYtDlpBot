using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Integration;

/// <summary>
/// Integration test for the download workflow:
/// Job dequeued → yt-dlp executes → Status updated → Emoji progresses
/// </summary>
public class DownloadWorkflowTests
{
    [Fact]
    public async Task DownloadJob_WhenSuccessful_UpdatesStatusAndAppliesCompleteEmoji()
    {
        // Arrange
        // TODO: Setup in-memory SQLite, mock yt-dlp executor
        var queue = new DownloadQueue();
        var executor = new LocalYtDlpExecutor();
        var monitor = new TelegramMonitor();
        
        const long messageId = 12345;
        const string url = "https://example.com/video";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            // 1. Dequeue job
            var job = await queue.DequeueAsync(cts.Token);
            
            // 2. Mark in progress and apply processing emoji
            await queue.MarkInProgressAsync(job!.JobId, cts.Token);
            await monitor.SetReactionAsync(messageId, "⚙️", cts.Token);
            
            // 3. Execute download
            var outputPath = await executor.DownloadAsync(url, "/downloads", cts.Token);
            
            // 4. Mark completed and apply success emoji
            await queue.MarkCompletedAsync(job.JobId, outputPath, cts.Token);
            await monitor.SetReactionAsync(messageId, "✅", cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadJob_WhenMultipleForSameMessage_ProcessesSequentially()
    {
        // Arrange
        var queue = new DownloadQueue();
        var executor = new LocalYtDlpExecutor();
        
        const long messageId = 12346;
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            // Process first job
            var job1 = await queue.DequeueAsync(cts.Token);
            await queue.MarkInProgressAsync(job1!.JobId, cts.Token);
            var output1 = await executor.DownloadAsync(job1.Url, "/downloads", cts.Token);
            await queue.MarkCompletedAsync(job1.JobId, output1, cts.Token);
            
            // Process second job
            var job2 = await queue.DequeueAsync(cts.Token);
            await queue.MarkInProgressAsync(job2!.JobId, cts.Token);
            var output2 = await executor.DownloadAsync(job2.Url, "/downloads", cts.Token);
            await queue.MarkCompletedAsync(job2.JobId, output2, cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
