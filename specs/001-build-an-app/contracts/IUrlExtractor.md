# Service Contract: IUrlExtractor

**Purpose**: Extract and validate URLs from Telegram message text.

**Responsibilities**:
- Parse message text for HTTP/HTTPS URLs
- Validate URL format and scheme
- Return list of valid URLs for download processing
- Handle malformed or invalid URLs gracefully

---

## Interface Definition

```csharp
namespace TelegramYtDlpBot.Services;

/// <summary>
/// Extracts and validates URLs from message text.
/// </summary>
public interface IUrlExtractor
{
    /// <summary>
    /// Extract all valid URLs from message text.
    /// </summary>
    /// <param name="messageText">Raw message text</param>
    /// <returns>List of valid HTTP/HTTPS URLs (empty if none found)</returns>
    List<string> ExtractUrls(string messageText);
    
    /// <summary>
    /// Validate if a URL is supported for download.
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if URL is valid HTTP/HTTPS scheme, false otherwise</returns>
    bool IsValidUrl(string url);
}
```

---

## Behavior Specifications

### ExtractUrls

**Preconditions**:
- Message text is not null (empty string allowed)

**Behavior**:
1. Find all URL patterns in text using regex: `https?://[^\s]+`
2. For each match: Validate using `IsValidUrl`
3. Return list of valid URLs (deduplicated, preserve order)
4. If no URLs found: Return empty list (not null)

**Postconditions**:
- All returned URLs have valid HTTP/HTTPS scheme
- Duplicate URLs removed (case-insensitive comparison)
- Order preserved (first occurrence wins for duplicates)

**Error Handling**:
- Empty/whitespace text: Return empty list
- Malformed URLs: Skip and continue (no exception)
- Oversized URLs (>2048 chars): Skip and log warning

---

### IsValidUrl

**Preconditions**:
- URL string is not null

**Behavior**:
1. Attempt to parse using `Uri.TryCreate` with `UriKind.Absolute`
2. Check scheme is `http` or `https` (case-insensitive)
3. Check URL length ≤ 2048 characters
4. Return true if all checks pass, false otherwise

**Postconditions**:
- Returns boolean (never throws exception)

**Error Handling**:
- Null URL: Return false
- Invalid scheme (ftp, file, etc.): Return false
- Relative URL: Return false
- Malformed URL: Return false

---

## Configuration Requirements

**None** - Pure utility service with no external configuration.

---

## Dependencies

- `ILogger<UrlExtractor>` (optional, for logging skipped URLs)

---

## Testing Strategy

### Unit Tests

**Test Cases**:
- ExtractUrls_WithSingleUrl_ReturnsSingleUrl
- ExtractUrls_WithMultipleUrls_ReturnsAll
- ExtractUrls_WithDuplicateUrls_ReturnsDeduplicated
- ExtractUrls_WithNoUrls_ReturnsEmptyList
- ExtractUrls_WithMixedValidInvalid_ReturnsOnlyValid
- ExtractUrls_WithHttpAndHttps_ReturnsBoth
- IsValidUrl_WithValidHttp_ReturnsTrue
- IsValidUrl_WithValidHttps_ReturnsTrue
- IsValidUrl_WithFtpScheme_ReturnsFalse
- IsValidUrl_WithRelativeUrl_ReturnsFalse
- IsValidUrl_WithMalformed_ReturnsFalse
- IsValidUrl_WithOversizedUrl_ReturnsFalse

**Test Data**:
```csharp
[Theory]
[InlineData("Check this https://youtube.com/watch?v=123", 1)]
[InlineData("http://example.com and https://test.com", 2)]
[InlineData("No URLs here", 0)]
[InlineData("https://youtube.com https://youtube.com", 1)] // duplicate
[InlineData("ftp://invalid.com", 0)] // wrong scheme
public void ExtractUrls_VariousInputs_ReturnsExpectedCount(string text, int expectedCount)
{
    var urls = _extractor.ExtractUrls(text);
    Assert.Equal(expectedCount, urls.Count);
}
```

---

## Performance Requirements

- **Latency**: Extract URLs from 1000-char message in <10ms
- **Memory**: No string allocations beyond result list
- **Throughput**: Process 1000 messages/second (not a bottleneck)

---

## URL Parsing Regex

**Pattern**: `https?://[^\s]+`

**Explanation**:
- `https?` - Match "http" or "https"
- `://` - Literal colon and slashes
- `[^\s]+` - One or more non-whitespace characters

**Limitations**:
- Does not validate TLD or DNS resolution
- Does not check for balanced parentheses/brackets
- Over-matches (e.g., URLs with trailing punctuation like `https://example.com.`)
- Post-processing via `IsValidUrl` cleans up false positives

**Alternative**: `System.Text.RegularExpressions.Regex` with compiled option for performance

---

## Example Usage

```csharp
public class MessageProcessor
{
    private readonly IUrlExtractor _extractor;
    
    public MessageProcessor(IUrlExtractor extractor)
    {
        _extractor = extractor;
    }
    
    public void ProcessMessage(string messageText)
    {
        var urls = _extractor.ExtractUrls(messageText);
        
        if (urls.Count == 0)
        {
            _logger.LogInformation("No URLs found in message");
            return;
        }
        
        foreach (var url in urls)
        {
            _logger.LogInformation("Found URL: {Url}", url);
            // Queue for download
        }
    }
}
```

---

## Edge Cases

### URLs with Markdown formatting
**Input**: `[Link](https://example.com)`  
**Behavior**: Extract `https://example.com)` → Validation fails due to trailing `)` → Enhance regex to handle

**Fix**: Use lookbehind to exclude trailing punctuation:
```regex
https?://[^\s]+(?<![.,;:!?)])
```

### URLs with query parameters
**Input**: `https://youtube.com/watch?v=abc&t=10s`  
**Behavior**: Extract full URL including parameters → Valid

### URLs with URL-encoded characters
**Input**: `https://example.com/path%20with%20spaces`  
**Behavior**: Extract full URL → Valid (Uri.TryCreate handles encoding)

### URLs with authentication
**Input**: `https://user:pass@example.com`  
**Behavior**: Extract full URL → Valid (but may fail yt-dlp download if auth invalid)

---

**Contract Ownership**: UrlExtractor service implementation  
**Review Status**: Draft - pending implementation
