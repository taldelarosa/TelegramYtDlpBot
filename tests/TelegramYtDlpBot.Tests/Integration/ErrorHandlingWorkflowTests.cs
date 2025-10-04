using FluentAssertions;
using Moq;
using TelegramYtDlpBot.Persistence;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Integration;

/// <summary>
/// Integration test for error handling and retry logic.
/// </summary>
public class ErrorHandlingWorkflowTests
{
    [Fact]
    public async Task DownloadJob_WhenFails_MarksFailedAndAppliesErrorEmoji()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        var executor = new LocalYtDlpExecutor();
        var monitor = new TelegramMonitor();
        
        const long messageId = 12345;
        const string invalidUrl = "https://invalid-url.com/video";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            // 1. Dequeue job
            var job = await queue.DequeueAsync(cts.Token);
            
            // 2. Mark in progress
            await queue.MarkInProgressAsync(job!.JobId, cts.Token);
            await monitor.SetReactionAsync(messageId, "⚙️", cts.Token);
            
            // 3. Attempt download (should fail)
            try
            {
                await executor.DownloadAsync(invalidUrl, "/downloads", cts.Token);
            }
            catch (YtDlpException ex)
            {
                // 4. Mark failed
                await queue.MarkFailedAsync(job.JobId, ex.Message, cts.Token);
            }
            
            // 5. Retry job
            var retried = await queue.RetryJobAsync(job.JobId, cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadJob_AfterMaxRetries_AppliesErrorEmoji()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        var monitor = new TelegramMonitor();
        
        const long messageId = 12345;
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            // Simulate 3 failed retries
            for (int i = 0; i < 3; i++)
            {
                await queue.MarkFailedAsync(jobId, "Error", cts.Token);
                var retried = await queue.RetryJobAsync(jobId, cts.Token);
                
                if (!retried)
                {
                    // Max retries reached, apply error emoji
                    await monitor.SetReactionAsync(messageId, "❌", cts.Token);
                    break;
                }
            }
        };

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadJob_WhenRetrySucceeds_AppliesCompleteEmoji()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        var executor = new LocalYtDlpExecutor();
        var monitor = new TelegramMonitor();
        
        const long messageId = 12345;
        const string url = "https://example.com/video";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            // First attempt fails
            var job = await queue.DequeueAsync(cts.Token);
            await queue.MarkInProgressAsync(job!.JobId, cts.Token);
            await queue.MarkFailedAsync(job.JobId, "Network error", cts.Token);
            
            // Retry
            var retried = await queue.RetryJobAsync(job.JobId, cts.Token);
            
            // Second attempt succeeds
            job = await queue.DequeueAsync(cts.Token);
            await queue.MarkInProgressAsync(job!.JobId, cts.Token);
            var outputPath = await executor.DownloadAsync(url, "/downloads", cts.Token);
            await queue.MarkCompletedAsync(job.JobId, outputPath, cts.Token);
            await monitor.SetReactionAsync(messageId, "✅", cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
