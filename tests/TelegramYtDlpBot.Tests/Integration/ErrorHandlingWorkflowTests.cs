using FluentAssertions;
using Moq;
using TelegramYtDlpBot.Models;
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
    public async Task DownloadJob_WhenFails_MarksFailedStatus()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        
        var jobId = Guid.NewGuid();
        const string errorMessage = "Download failed";
        using var cts = new CancellationTokenSource();

        // Act
        await queue.MarkFailedAsync(jobId, errorMessage, cts.Token);

        // Assert
        mockStateManager.Verify(m => m.UpdateJobStatusAsync(
            jobId,
            JobStatus.Failed,
            errorMessage,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadQueue_CanMarkCompleted()
    {
        // Arrange
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        
        var jobId = Guid.NewGuid();
        const string outputPath = "/downloads/video.mp4";
        using var cts = new CancellationTokenSource();

        // Act
        await queue.MarkCompletedAsync(jobId, outputPath, cts.Token);

        // Assert
        mockStateManager.Verify(m => m.UpdateJobStatusAsync(
            jobId,
            JobStatus.Completed,
            null,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TelegramMonitor_SetReaction_WithoutBotClient_ReturnsFalse()
    {
        // Arrange
        var monitor = new TelegramMonitor(); // Parameterless constructor - no bot client
        const long messageId = 12345;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await monitor.SetReactionAsync(messageId, "✅", cts.Token);

        // Assert
        result.Should().BeFalse(); // No bot client initialized
    }
}
