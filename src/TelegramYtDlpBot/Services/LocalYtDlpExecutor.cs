using System.Diagnostics;
using System.Text;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Executes yt-dlp downloads using a local yt-dlp executable.
/// </summary>
public class LocalYtDlpExecutor : IYtDlpExecutor
{
    private readonly string _executablePath;
    private const int TimeoutSeconds = 3600; // 1 hour default timeout

    public LocalYtDlpExecutor(string? executablePath = null)
    {
        _executablePath = executablePath ?? "yt-dlp";
    }

    /// <inheritdoc />
    public async Task<string> DownloadAsync(string url, string outputPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);

        // Build output template: /downloads/%(title)s-%(id)s.%(ext)s
        var outputTemplate = Path.Combine(outputPath, "%(title)s-%(id)s.%(ext)s");
        
        // yt-dlp arguments
        var arguments = $"--no-playlist --output \"{outputTemplate}\" \"{url}\"";

        var stdOutput = new StringBuilder();
        var stdError = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = outputPath
        };

        using var process = new Process { StartInfo = startInfo };
        
        // Capture output and error streams
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdOutput.AppendLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdError.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with cancellation support
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorMessage = stdError.Length > 0 
                    ? stdError.ToString().Trim() 
                    : $"yt-dlp exited with code {process.ExitCode}";
                throw new YtDlpException($"Download failed: {errorMessage}");
            }

            // Parse output to find the downloaded file path
            var outputFilePath = ExtractDownloadedFilePath(stdOutput.ToString(), outputPath);
            
            if (string.IsNullOrEmpty(outputFilePath) || !File.Exists(outputFilePath))
            {
                throw new YtDlpException("Download completed but output file not found");
            }

            return outputFilePath;
        }
        catch (OperationCanceledException)
        {
            // Kill process if cancellation requested
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* Best effort */ }
            }
            throw;
        }
        catch (Exception ex) when (ex is not YtDlpException and not OperationCanceledException)
        {
            throw new YtDlpException($"Failed to execute yt-dlp: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            
            await process.WaitForExitAsync(cancellationToken);
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract the downloaded file path from yt-dlp output.
    /// Looks for lines like "[download] Destination: /path/to/file.mp4"
    /// or "[Merger] Merging formats into /path/to/file.mkv"
    /// </summary>
    private static string? ExtractDownloadedFilePath(string output, string outputDirectory)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        // Common patterns in yt-dlp output
        var patterns = new[]
        {
            @"\[download\] Destination: (.+)",
            @"\[download\] (.+) has already been downloaded",
            @"\[Merger\] Merging formats into (.+)",
            @"Deleting original file (.+)",
        };

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        // Search from the end (most recent operations)
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(lines[i], pattern);
                if (match.Success)
                {
                    var filePath = match.Groups[1].Value.Trim();
                    
                    // If it's a relative path, combine with output directory
                    if (!Path.IsPathRooted(filePath))
                    {
                        filePath = Path.Combine(outputDirectory, filePath);
                    }
                    
                    return filePath;
                }
            }
        }

        // Fallback: search for any file that was created in the output directory
        // This is a best-effort approach if we can't parse the output
        try
        {
            var files = Directory.GetFiles(outputDirectory, "*.*", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                // Return the most recently created file
                return files.OrderByDescending(f => File.GetCreationTime(f)).FirstOrDefault();
            }
        }
        catch
        {
            // Ignore directory access errors
        }

        return null;
    }
}
