namespace TelegramYtDlpBot.Services;

/// <summary>
/// Executes yt-dlp downloads (local or remote mode).
/// </summary>
public interface IYtDlpExecutor
{
    /// <summary>
    /// Download a URL using yt-dlp.
    /// </summary>
    /// <param name="url">URL to download</param>
    /// <param name="outputPath">Base output directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to downloaded file</returns>
    /// <exception cref="YtDlpException">Thrown when download fails</exception>
    Task<string> DownloadAsync(string url, string outputPath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Check if yt-dlp is available and working.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if yt-dlp is accessible, false otherwise</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Exception thrown when yt-dlp execution fails.
/// </summary>
public class YtDlpException : Exception
{
    public YtDlpException(string message) : base(message) { }
    public YtDlpException(string message, Exception innerException) : base(message, innerException) { }
}
