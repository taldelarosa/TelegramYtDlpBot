using System.Text.RegularExpressions;

namespace TelegramYtDlpBot.Services;

public class UrlExtractor : IUrlExtractor
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+(?<![.,;:!?)])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private const int MaxUrlLength = 2048;

    public IReadOnlyList<string> ExtractUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var matches = UrlRegex.Matches(text);
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var url = match.Value;
            
            // Validate and add to set (deduplication)
            if (IsValidUrl(url))
            {
                urls.Add(url);
            }
        }

        return urls.ToList();
    }

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (url.Length > MaxUrlLength)
            return false;

        // Use Uri.TryCreate for validation
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Only allow HTTP and HTTPS schemes
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
