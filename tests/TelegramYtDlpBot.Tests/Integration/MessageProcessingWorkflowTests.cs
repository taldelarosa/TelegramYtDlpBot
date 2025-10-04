using FluentAssertions;
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
        // TODO: Setup in-memory SQLite, mock Telegram client
        var urlExtractor = new UrlExtractor();
        var queue = new DownloadQueue();
        var monitor = new TelegramMonitor();
        
        const string messageText = "Check out this video: https://youtube.com/watch?v=test";
        const long messageId = 12345;
        using var cts = new CancellationTokenSource();

        // Act
        var act = () =>
        {
            // 1. Extract URLs
            var urls = urlExtractor.ExtractUrls(messageText);
            
            // 2. Queue jobs
            foreach (var url in urls)
            {
                queue.EnqueueAsync(messageId, url, cts.Token);
            }
            
            // 3. Apply seen emoji
            monitor.SetReactionAsync(messageId, "👀", cts.Token);
        };

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public async Task MessageReceived_WithMultipleUrls_CreatesMultipleJobs()
    {
        // Arrange
        var urlExtractor = new UrlExtractor();
        var queue = new DownloadQueue();
        
        const string messageText = "First: https://youtube.com/1 Second: https://vimeo.com/2";
        const long messageId = 12346;
        using var cts = new CancellationTokenSource();

        // Act
        var act = () =>
        {
            var urls = urlExtractor.ExtractUrls(messageText);
            foreach (var url in urls)
            {
                queue.EnqueueAsync(messageId, url, cts.Token);
            }
        };

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}
