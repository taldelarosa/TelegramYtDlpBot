namespace TelegramYtDlpBot.Services;

/// <summary>
/// Extracts and validates URLs from text content.
/// </summary>
public interface IUrlExtractor
{
    /// <summary>
    /// Extract all valid URLs from the given text.
    /// </summary>
    /// <param name="text">Text to parse for URLs</param>
    /// <returns>List of unique, valid URLs found in the text</returns>
    IReadOnlyList<string> ExtractUrls(string text);
    
    /// <summary>
    /// Check if a URL is valid and supported.
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if URL is valid and supported, false otherwise</returns>
    bool IsValidUrl(string url);
}
