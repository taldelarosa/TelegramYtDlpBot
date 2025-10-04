using FluentAssertions;
using Moq;
using TelegramYtDlpBot.Models;
using TelegramYtDlpBot.Persistence;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Integration;

/// <summary>
/// Integration test for the complete workflow:
/// Message received → URL extracted → Job queued → Emoji applied
/// </summary>
public class MessageProcessingWorkflowTests
{
    [Fact]
    public async Task MessageReceived_WithUrl_CreatesQueuedJobAndAppliesSeenEmoji()
    {
        // Arrange
        var urlExtractor = new UrlExtractor();
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        var monitor = new TelegramMonitor(); // Uses parameterless constructor (no bot client)
        
        const string messageText = "Check out this video: https://youtube.com/watch?v=test";
        const long messageId = 12345;
        using var cts = new CancellationTokenSource();

        // Act
        // 1. Extract URLs
        var urls = urlExtractor.ExtractUrls(messageText);
        
        // 2. Queue jobs
        foreach (var url in urls)
        {
            await queue.EnqueueAsync(messageId, url, cts.Token);
        }
        
        // 3. Apply seen emoji (will return false since no bot client)
        var reactionSet = await monitor.SetReactionAsync(messageId, "👀", cts.Token);

        // Assert
        urls.Should().HaveCount(1);
        urls[0].Should().Be("https://youtube.com/watch?v=test");
        
        // Verify job was queued
        mockStateManager.Verify(m => m.SaveJobAsync(
            It.Is<DownloadJob>(j => j.MessageId == messageId && j.Url == urls[0]),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Reaction returns false since no bot client initialized
        reactionSet.Should().BeFalse();
    }

    [Fact]
    public async Task MessageReceived_WithMultipleUrls_CreatesMultipleJobs()
    {
        // Arrange
        var urlExtractor = new UrlExtractor();
        var mockStateManager = new Mock<IStateManager>();
        var queue = new DownloadQueue(mockStateManager.Object);
        
        const string messageText = "First: https://youtube.com/1 Second: https://vimeo.com/2";
        const long messageId = 12346;
        using var cts = new CancellationTokenSource();

        // Act
        var urls = urlExtractor.ExtractUrls(messageText);
        foreach (var url in urls)
        {
            await queue.EnqueueAsync(messageId, url, cts.Token);
        }

        // Assert
        urls.Should().HaveCount(2);
        urls.Should().Contain("https://youtube.com/1");
        urls.Should().Contain("https://vimeo.com/2");
        
        // Verify both jobs were queued
        mockStateManager.Verify(m => m.SaveJobAsync(
            It.IsAny<DownloadJob>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
