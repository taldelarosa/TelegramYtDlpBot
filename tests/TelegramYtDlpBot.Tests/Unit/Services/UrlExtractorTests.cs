using FluentAssertions;
using TelegramYtDlpBot.Services;
using Xunit;

namespace TelegramYtDlpBot.Tests.Unit.Services;

public class UrlExtractorTests
{
    [Fact]
    public void ExtractUrls_WithSingleUrl_ReturnsSingleUrl()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "Check this out: https://example.com/video";

        // Act
        var result = extractor.ExtractUrls(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("https://example.com/video");
    }

    [Fact]
    public void ExtractUrls_WithMultipleUrls_ReturnsAll()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "First: https://youtube.com/watch?v=abc Second: https://vimeo.com/123";

        // Act
        var result = extractor.ExtractUrls(text);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("https://youtube.com/watch?v=abc");
        result.Should().Contain("https://vimeo.com/123");
    }

    [Fact]
    public void ExtractUrls_WithDuplicateUrls_ReturnsDeduplicated()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "https://example.com https://example.com https://other.com";

        // Act
        var result = extractor.ExtractUrls(text);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("https://example.com");
        result.Should().Contain("https://other.com");
    }

    [Fact]
    public void ExtractUrls_WithNoUrls_ReturnsEmptyList()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "This text has no URLs at all";

        // Act
        var result = extractor.ExtractUrls(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractUrls_WithMixedValidInvalid_ReturnsOnlyValid()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "Good: https://example.com Bad: not-a-url Invalid: ftp://old.site";

        // Act
        var result = extractor.ExtractUrls(text);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("https://example.com");
    }

    [Fact]
    public void IsValidUrl_WithValidHttp_ReturnsTrue()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "http://example.com/video";

        // Act
        var result = extractor.IsValidUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidUrl_WithValidHttps_ReturnsTrue()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "https://youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var result = extractor.IsValidUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidUrl_WithFtpScheme_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "ftp://files.example.com/video.mp4";

        // Act
        var result = extractor.IsValidUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUrl_WithRelativeUrl_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "/relative/path/video";

        // Act
        var result = extractor.IsValidUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUrl_WithMalformed_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "ht!tp://mal formed .com";

        // Act
        var result = extractor.IsValidUrl(url);

        // Assert
        result.Should().BeFalse();
    }
}
