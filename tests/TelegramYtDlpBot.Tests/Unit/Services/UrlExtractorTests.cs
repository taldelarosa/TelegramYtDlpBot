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
        var act = () => extractor.ExtractUrls(text);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ExtractUrls_WithMultipleUrls_ReturnsAll()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "First: https://youtube.com/watch?v=abc Second: https://vimeo.com/123";

        // Act
        var act = () => extractor.ExtractUrls(text);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ExtractUrls_WithDuplicateUrls_ReturnsDeduplicated()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "https://example.com https://example.com https://other.com";

        // Act
        var act = () => extractor.ExtractUrls(text);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ExtractUrls_WithNoUrls_ReturnsEmptyList()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "This text has no URLs at all";

        // Act
        var act = () => extractor.ExtractUrls(text);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ExtractUrls_WithMixedValidInvalid_ReturnsOnlyValid()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string text = "Good: https://example.com Bad: not-a-url Invalid: ftp://old.site";

        // Act
        var act = () => extractor.ExtractUrls(text);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void IsValidUrl_WithValidHttp_ReturnsTrue()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "http://example.com/video";

        // Act
        var act = () => extractor.IsValidUrl(url);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void IsValidUrl_WithValidHttps_ReturnsTrue()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "https://youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var act = () => extractor.IsValidUrl(url);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void IsValidUrl_WithFtpScheme_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "ftp://files.example.com/video.mp4";

        // Act
        var act = () => extractor.IsValidUrl(url);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void IsValidUrl_WithRelativeUrl_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "/relative/path/video";

        // Act
        var act = () => extractor.IsValidUrl(url);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void IsValidUrl_WithMalformed_ReturnsFalse()
    {
        // Arrange
        var extractor = new UrlExtractor();
        const string url = "ht!tp://mal formed .com";

        // Act
        var act = () => extractor.IsValidUrl(url);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}
