# Service Contract: IYtDlpExecutor

**Purpose**: Execute yt-dlp downloads in local or remote mode.

**Responsibilities**:
- Download media files from URLs using yt-dlp
- Support both local CLI execution and remote API calls
- Capture output and error information
- Handle timeouts and cancellation
- Return path to downloaded file

---

## Interface Definition

```csharp
namespace TelegramYtDlpBot.Services;

/// <summary>
/// Executes yt-dlp downloads (local or remote mode).
/// </summary>
public interface IYtDlpExecutor
{
    /// <summary>
    /// Download media from URL.
    /// </summary>
    /// <param name="url">Source URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to downloaded file</returns>
    /// <exception cref="YtDlpException">Download failed</exception>
    Task<string> DownloadAsync(string url, CancellationToken cancellationToken);
    
    /// <summary>
    /// Check if yt-dlp is available and functional.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if yt-dlp responds correctly</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Exception thrown when yt-dlp download fails.
/// </summary>
public class YtDlpException : Exception
{
    public string Url { get; }
    public string? StdOut { get; }
    public string? StdErr { get; }
    public int? ExitCode { get; }
    
    public YtDlpException(string message, string url, string? stdOut = null, string? stdErr = null, int? exitCode = null)
        : base(message)
    {
        Url = url;
        StdOut = stdOut;
        StdErr = stdErr;
        ExitCode = exitCode;
    }
}
```

---

## Implementations

### LocalYtDlpExecutor

Uses `System.Diagnostics.Process` to execute yt-dlp CLI.

**Behavior**:
1. Build command line arguments:
   ```
   yt-dlp {url} -o {outputTemplate} --format {quality} [--config-location {configPath}]
   ```
2. Start process with stdout/stderr redirection
3. Wait for completion with timeout (default 60 minutes)
4. If exit code 0: Parse stdout for output file path, return
5. If exit code ≠ 0: Throw YtDlpException with stderr

**HealthCheck**:
- Execute `yt-dlp --version`
- Check exit code 0 and version output format

---

### RemoteYtDlpExecutor

Uses `HttpClient` to call self-hosted yt-dlp API.

**Behavior**:
1. POST to `{baseUrl}/download`:
   ```json
   {
     "url": "https://...",
     "format": "bestvideo+bestaudio",
     "outputTemplate": "..."
   }
   ```
2. Await response with timeout
3. If HTTP 200: Parse response for download URL, fetch file to local storage, return path
4. If HTTP 4xx/5xx: Throw YtDlpException with response body

**HealthCheck**:
- GET `{baseUrl}/health`
- Check HTTP 200 response

---

## Behavior Specifications

### DownloadAsync (Local Mode)

**Preconditions**:
- yt-dlp executable exists at configured path
- Download path is writable
- URL is valid (validated by caller)

**Behavior**:
1. Generate output filename using template
2. Execute yt-dlp process:
   ```bash
   yt-dlp "https://example.com/video" \
     -o "/data/downloads/%(uploader)s/%(upload_date)s/%(title)s.%(ext)s" \
     --format "bestvideo+bestaudio/best" \
     --config-location "/config/yt-dlp.conf"  # if configured
   ```
3. Capture stdout/stderr streams
4. Wait for process exit with timeout (1 hour default)
5. Parse stdout for line: `[download] Destination: {path}`
6. Verify file exists at path
7. Return absolute path to downloaded file

**Postconditions**:
- File exists on filesystem at returned path
- File size > 0 bytes

**Error Handling**:
- Exit code ≠ 0: Parse stderr for error (e.g., "ERROR: Unsupported URL"), throw YtDlpException
- Timeout exceeded: Kill process, throw YtDlpException ("Download timeout")
- File not found after success: Throw YtDlpException ("Output file not found")
- Cancellation requested: Kill process, throw OperationCanceledException

---

### DownloadAsync (Remote Mode)

**Preconditions**:
- Remote API URL is configured and reachable
- API key (if required) is configured

**Behavior**:
1. Send HTTP POST to remote API:
   ```http
   POST /download HTTP/1.1
   Host: yt-dlp-api.local
   Authorization: Bearer {apiKey}  # if configured
   Content-Type: application/json
   
   {
     "url": "https://example.com/video",
     "format": "bestvideo+bestaudio/best",
     "outputTemplate": "%(title)s.%(ext)s"
   }
   ```
2. Await response with timeout (1 hour default)
3. Parse response JSON:
   ```json
   {
     "success": true,
     "downloadUrl": "https://yt-dlp-api.local/downloads/abc123.mp4",
     "filename": "MyVideo.mp4"
   }
   ```
4. Download file from `downloadUrl` to local storage
5. Return local file path

**Postconditions**:
- File downloaded to local storage
- File size > 0 bytes

**Error Handling**:
- HTTP 4xx: Parse error message, throw YtDlpException
- HTTP 5xx: Server error, throw YtDlpException
- Network timeout: Throw YtDlpException
- Cancellation requested: Abort HTTP request, throw OperationCanceledException

---

### HealthCheckAsync

**Behavior (Local)**:
- Execute `yt-dlp --version`
- Parse stdout for version string (e.g., "2024.01.01")
- Return true if exit code 0, false otherwise

**Behavior (Remote)**:
- GET `{baseUrl}/health`
- Return true if HTTP 200, false otherwise

**Error Handling**:
- Do not throw exceptions (return false on error)
- Log warnings for health check failures

---

## Configuration Requirements

**From BotConfiguration.YtDlp**:
- `Mode` (enum): Local or Remote
- `ExecutablePath` (string, for Local): Path to yt-dlp binary
- `RemoteApiUrl` (string, for Remote): Base URL of remote API
- `RemoteApiKey` (string?, for Remote): Optional API authentication key
- `Quality` (string): Format selector (e.g., "bestvideo+bestaudio/best")
- `OutputTemplate` (string): yt-dlp output template
- `ConfigFilePath` (string?, optional): Path to yt-dlp config file
- `TimeoutMinutes` (int, default 60): Max download duration

**From BotConfiguration.Storage**:
- `DownloadPath` (string): Base directory for downloaded files

---

## Dependencies

- `System.Diagnostics.Process` (for local mode)
- `HttpClient` (for remote mode)
- `ILogger<LocalYtDlpExecutor>` or `ILogger<RemoteYtDlpExecutor>`
- `IOptions<BotConfiguration>`

---

## Testing Strategy

### Unit Tests (Local Mode)

**Mocking**: Mock Process execution (difficult - use integration tests instead)

**Test Cases**:
- DownloadAsync_WithValidUrl_ReturnsFilePath
- DownloadAsync_WithInvalidUrl_ThrowsYtDlpException
- DownloadAsync_WithTimeout_ThrowsYtDlpException
- DownloadAsync_WithCancellation_ThrowsOperationCanceledException
- HealthCheck_WithValidExecutable_ReturnsTrue
- HealthCheck_WithMissingExecutable_ReturnsFalse

### Integration Tests (Local Mode)

**Real yt-dlp**: Use test URLs (e.g., YouTube test video)

**Test Cases**:
- RealDownload_WithYouTubeUrl_DownloadsSuccessfully
- RealDownload_WithUnsupportedSite_ThrowsException

### Unit Tests (Remote Mode)

**Mocking**: Mock HttpClient responses

**Test Cases**:
- DownloadAsync_WithSuccessResponse_DownloadsFile
- DownloadAsync_With404_ThrowsYtDlpException
- DownloadAsync_With500_ThrowsYtDlpException
- HealthCheck_With200_ReturnsTrue
- HealthCheck_With503_ReturnsFalse

---

## Performance Requirements

- **Latency**: Initiate download within 1 second
- **Timeout**: Configurable, default 1 hour per download
- **Memory**: Stream files to disk (no full file buffering in memory)
- **Throughput**: Sequential (one download at a time by design)

---

## Example Usage

```csharp
public class DownloadWorker
{
    private readonly IYtDlpExecutor _executor;
    
    public async Task ProcessJob(DownloadJob job, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = await _executor.DownloadAsync(job.Url, cancellationToken);
            _logger.LogInformation("Downloaded {Url} to {Path}", job.Url, filePath);
            return filePath;
        }
        catch (YtDlpException ex)
        {
            _logger.LogError(ex, "Download failed: {Url} - {Error}", ex.Url, ex.Message);
            throw;
        }
    }
}
```

---

**Contract Ownership**: LocalYtDlpExecutor and RemoteYtDlpExecutor implementations  
**Review Status**: Draft - pending implementation
