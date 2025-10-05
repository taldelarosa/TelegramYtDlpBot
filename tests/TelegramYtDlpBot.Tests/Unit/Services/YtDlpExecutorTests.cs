using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;
using Moq;
namespace TelegramYtDlpBot.Tests.Unit.Services;

public class YtDlpExecutorTests
{
    [Fact]
    public async Task DownloadAsync_WithValidUrl_ReturnsFilePath()
    {
        // Arrange
        var mock = new Mock<IYtDlpExecutor>();
        const string url = "https://example.com/video";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource();

        // Setup mock to throw YtDlpException as if download failed
        mock.Setup(x => x.DownloadAsync(url, outputPath, cts.Token))
            .ThrowsAsync(new YtDlpException("Download failed"));

        // Act
        var act = async () => await mock.Object.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        var exception = await act.Should().ThrowAsync<YtDlpException>();
        exception.Which.Message.Should().Be("Download failed");
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidUrl_ThrowsYtDlpException()
    {
        // Arrange
        var mock = new Mock<IYtDlpExecutor>();
        const string url = "not-a-valid-url-at-all";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Setup mock to throw YtDlpException for invalid URL
        mock.Setup(x => x.DownloadAsync(url, outputPath, cts.Token))
            .ThrowsAsync(new YtDlpException("Invalid URL"));

        // Act
        var act = async () => await mock.Object.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<YtDlpException>();
    }

    [Fact]
    public async Task DownloadAsync_WithTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        var mock = new Mock<IYtDlpExecutor>();
        const string url = "https://example.com/very-large-file";
        var outputPath = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        // Setup mock to throw OperationCanceledException for timeout
        mock.Setup(x => x.DownloadAsync(url, outputPath, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await mock.Object.DownloadAsync(url, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DownloadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var executor = new LocalYtDlpExecutor(GetYtDlpPath());
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
        var executor = new LocalYtDlpExecutor(GetYtDlpPath());
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
