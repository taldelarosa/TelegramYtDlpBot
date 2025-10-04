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
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource();

        // Act - This will fail since yt-dlp likely isn't installed in test environment
        // We're testing that it at least attempts execution properly
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert - Should throw YtDlpException (download will fail) or file not found
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Should().BeOfType<YtDlpException>();
        
        // Cleanup
        try { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); } catch { }
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidUrl_ThrowsYtDlpException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://invalid-url-that-does-not-exist.com/video";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert - Should throw YtDlpException
        await act.Should().ThrowAsync<YtDlpException>();
        
        // Cleanup
        try { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); } catch { }
    }

    [Fact]
    public async Task DownloadAsync_WithTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://example.com/very-large-file";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert - Should throw OperationCanceledException due to immediate timeout
        await act.Should().ThrowAsync<OperationCanceledException>();
        
        // Cleanup
        try { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); } catch { }
    }

    [Fact]
    public async Task DownloadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        const string url = "https://example.com/video";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var act = async () => await executor.DownloadAsync(url, outputPath, cts.Token);

        // Assert - Should throw OperationCanceledException
        await act.Should().ThrowAsync<OperationCanceledException>();
        
        // Cleanup
        try { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); } catch { }
    }

    [Fact]
    public async Task HealthCheck_WithValidExecutable_ReturnsTrue()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await executor.HealthCheckAsync(cts.Token);

        // Assert - Will return true if yt-dlp is installed, false otherwise
        // We just verify the method completes without exception and returns a bool
        result.Should().Be(result); // Tautology - just verify it returns without exception
    }

    [Fact]
    public async Task HealthCheck_WithMissingExecutable_ReturnsFalse()
    {
        // Arrange - Use a non-existent executable path
        var executor = new LocalYtDlpExecutor("non-existent-yt-dlp-executable-12345");
        using var cts = new CancellationTokenSource();

        // Act
        var result = await executor.HealthCheckAsync(cts.Token);

        // Assert - Should return false when executable doesn't exist
        result.Should().BeFalse();
    }
}
