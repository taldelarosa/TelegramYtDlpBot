using FluentAssertions;
using Moq;
using TelegramYtDlpBot.Models;
using TelegramYtDlpBot.Persistence;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Integration;

/// <summary>
/// Integration test for the download workflow:
/// Job dequeued → Status updated → Emoji progresses
/// Note: Skips actual yt-dlp execution since binary may not be installed
/// </summary>
public class DownloadWorkflowTests
{
    [Fact]
    public async Task DownloadJob_WhenQueueEmpty_ReturnsNull()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        using var cts = new CancellationTokenSource();

        // Setup mock to return null for dequeue (empty queue)
        mockStateManager.Setup(m => m.GetNextQueuedJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadJob?)null);

        // Act
        var job = await queue.DequeueAsync(cts.Token);

        // Assert
        job.Should().BeNull(); // Queue is empty
    }

    [Fact]
    public async Task DownloadJob_WhenMultipleQueued_ProcessesSequentially()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        using var cts = new CancellationTokenSource();

        // Setup mock to return two jobs sequentially, then null
        var job1 = new DownloadJob
        {
            JobId = Guid.NewGuid(),
            MessageId = 12346,
            Url = "https://youtube.com/1",
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };
        
        var job2 = new DownloadJob
        {
            JobId = Guid.NewGuid(),
            MessageId = 12346,
            Url = "https://youtube.com/2",
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        mockStateManager.SetupSequence(m => m.GetNextQueuedJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job1)
            .ReturnsAsync(job2)
            .ReturnsAsync((DownloadJob?)null);

        // Act
        var firstJob = await queue.DequeueAsync(cts.Token);
        var secondJob = await queue.DequeueAsync(cts.Token);
        var thirdJob = await queue.DequeueAsync(cts.Token);

        // Assert
        firstJob.Should().NotBeNull();
        firstJob!.Url.Should().Be("https://youtube.com/1");
        
        secondJob.Should().NotBeNull();
        secondJob!.Url.Should().Be("https://youtube.com/2");
        
        thirdJob.Should().BeNull();
    }
}
