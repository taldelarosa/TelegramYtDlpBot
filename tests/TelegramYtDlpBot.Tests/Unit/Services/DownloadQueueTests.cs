using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Unit.Services;

public class DownloadQueueTests
{
    [Fact]
    public async Task EnqueueAsync_WithValidJob_InsertsToDatabase()
    {
        // Arrange
        var queue = new DownloadQueue();
        const long messageId = 12345;
        const string url = "https://example.com/video";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.EnqueueAsync(messageId, url, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DequeueAsync_WithEmptyQueue_ReturnsNull()
    {
        // Arrange
        var queue = new DownloadQueue();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.DequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DequeueAsync_WithQueuedJobs_ReturnsFIFO()
    {
        // Arrange
        var queue = new DownloadQueue();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.DequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task MarkInProgressAsync_WithQueuedJob_UpdatesStatus()
    {
        // Arrange
        var queue = new DownloadQueue();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.MarkInProgressAsync(jobId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task MarkCompletedAsync_WithInProgressJob_SetsOutputPath()
    {
        // Arrange
        var queue = new DownloadQueue();
        var jobId = Guid.NewGuid();
        const string outputPath = "/downloads/video.mp4";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.MarkCompletedAsync(jobId, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task MarkFailedAsync_WithInProgressJob_SetsErrorMessage()
    {
        // Arrange
        var queue = new DownloadQueue();
        var jobId = Guid.NewGuid();
        const string errorMessage = "Download failed: Network error";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.MarkFailedAsync(jobId, errorMessage, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task RetryJobAsync_WithFailedJob_RequeuesIfRetriesRemain()
    {
        // Arrange
        var queue = new DownloadQueue();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.RetryJobAsync(jobId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task RetryJobAsync_WithMaxRetries_ReturnsFalse()
    {
        // Arrange
        var queue = new DownloadQueue();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.RetryJobAsync(jobId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetStatsAsync_WithMixedJobs_ReturnsAccurateCounts()
    {
        // Arrange
        var queue = new DownloadQueue();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await queue.GetStatsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
