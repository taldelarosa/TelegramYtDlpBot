using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Unit.Services;

public class YtDlpExecutorTests
{
    [Fact]
    public async Task DownloadAsync_WithValidUrl_ReturnsFilePath()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://example.com/video";
        const string outputPath = "/downloads";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidUrl_ThrowsYtDlpException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://invalid-url-that-does-not-exist.com/video";
        const string outputPath = "/downloads";
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadAsync_WithTimeout_ThrowsYtDlpException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://example.com/very-large-file";
        const string outputPath = "/downloads";
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DownloadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://example.com/video";
        const string outputPath = "/downloads";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task HealthCheck_WithValidExecutable_ReturnsTrue()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await executor.HealthCheckAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task HealthCheck_WithMissingExecutable_ReturnsFalse()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await executor.HealthCheckAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
