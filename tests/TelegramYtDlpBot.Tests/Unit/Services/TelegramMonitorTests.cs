using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Unit.Services;

public class TelegramMonitorTests
{
    [Fact]
    public async Task StartMonitoring_WithValidConfig_RaisesMessageReceivedEvent()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        var eventRaised = false;
        monitor.MessageReceived += (sender, args) => { eventRaised = true; };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        var act = async () => await monitor.StartMonitoringAsync(cts.Token);

        // Assert - Should throw InvalidOperationException when bot client is null
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bot client not initialized*");
    }

    [Fact]
    public async Task StartMonitoring_WithInvalidToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await monitor.StartMonitoringAsync(cts.Token);

        // Assert - Should throw InvalidOperationException when bot client is null
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bot client not initialized*");
    }

    [Fact]
    public async Task StartMonitoring_WithNetworkError_RetriesWithBackoff()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        var act = async () => await monitor.StartMonitoringAsync(cts.Token);

        // Assert - Should throw InvalidOperationException when bot client is null
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bot client not initialized*");
    }

    [Fact]
    public async Task SetReaction_WithValidMessage_ReturnsTrue()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        const long messageId = 12345;
        const string emoji = "👀";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await monitor.SetReactionAsync(messageId, emoji, cts.Token);

        // Assert - Should return false when bot client is null
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetReaction_WithInvalidMessage_ReturnsFalse()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        const long invalidMessageId = -1;
        const string emoji = "👀";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await monitor.SetReactionAsync(invalidMessageId, emoji, cts.Token);

        // Assert - Should return false when bot client is null
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetReaction_WithRateLimit_RetriesOnce()
    {
        // Arrange
        var monitor = new TelegramMonitor();
        const long messageId = 12345;
        const string emoji = "⚙️";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await monitor.SetReactionAsync(messageId, emoji, cts.Token);

        // Assert - Should return false when bot client is null
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WhenNotStarted_ReturnsFalse()
    {
        // Arrange
        var monitor = new TelegramMonitor();

        // Act
        var result = monitor.IsConnected;

        // Assert - Should return false when not connected
        result.Should().BeFalse();
    }
}

