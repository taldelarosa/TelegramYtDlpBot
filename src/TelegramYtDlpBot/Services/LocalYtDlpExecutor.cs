using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Executes yt-dlp downloads using a local yt-dlp executable.
/// </summary>
public class LocalYtDlpExecutor : IYtDlpExecutor
{
    private readonly string _executablePath;
    private readonly ILogger<LocalYtDlpExecutor> _logger;
    private const int TimeoutSeconds = 3600; // 1 hour default timeout

    public LocalYtDlpExecutor(string? executablePath = null, ILogger<LocalYtDlpExecutor> logger)
    {
        _executablePath = executablePath ?? "yt-dlp";
        _logger = logger;
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
            // Check cancellation before starting process
            cancellationToken.ThrowIfCancellationRequested();
            
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
            var ytDlpOutput = stdOutput.ToString();
            var ytDlpError = stdError.ToString();
            
            var outputFilePath = ExtractDownloadedFilePath(ytDlpOutput, outputPath);
            
            if (string.IsNullOrEmpty(outputFilePath))
            {
                var errorDetails = $"Could not parse output file path from yt-dlp.\n" +
                                  $"Output directory: {outputPath}\n" +
                                  $"yt-dlp stdout:\n{ytDlpOutput}\n" +
                                  $"yt-dlp stderr:\n{ytDlpError}";
                throw new YtDlpException($"Download completed but output file path not found. {errorDetails}");
            }
            
            // If file doesn't exist, it might be because yt-dlp skipped it (already downloaded)
            if (!File.Exists(outputFilePath))
            {
                // Check if yt-dlp said it was already downloaded
                if (ytDlpOutput.Contains("has already been downloaded", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Duplicate video request detected for URL: {Url} - File already exists at: {FilePath}", url, outputFilePath);
                    // File was already downloaded, which is fine - return the expected path
                    return outputFilePath;
                }
                
                var errorDetails = $"File does not exist at path: {outputFilePath}\n" +
                                  $"Output directory: {outputPath}\n" +
                                  $"yt-dlp stdout:\n{ytDlpOutput}\n" +
                                  $"yt-dlp stderr:\n{ytDlpError}";
                throw new YtDlpException($"Download completed but output file not found. {errorDetails}");
            }

            return outputFilePath;
        }
        catch (OperationCanceledException)
        {
            // Kill process if cancellation requested and process was started
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort - process may not have started or already exited
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
                    
                    // Check if the path already starts with the output directory name
                    // yt-dlp outputs relative paths like "downloads\file.mp4" when we pass "./downloads" as output path
                    var outputDirName = Path.GetFileName(Path.GetFullPath(outputDirectory));
                    if (filePath.StartsWith(outputDirName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        filePath.StartsWith(outputDirName + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        // The path already includes the output directory, just use it directly
                        return filePath;
                    }
                    
                    // If it's a relative path that doesn't include the directory, combine with output directory
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
            var files = Directory.GetFiles(outputDirectory, "*.*", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                // Return the most recently modified file (more reliable than creation time)
                return files.OrderByDescending(f => File.GetLastWriteTimeUtc(f)).FirstOrDefault();
            }
        }
        catch
        {
            // Ignore directory access errors
        }

        return null;
    }
    
    /// <summary>
    /// Compute MD5 hash of a file.
    /// </summary>
    private static string ComputeFileHash(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = md5.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    /// <summary>
    /// Check if two files have the same content based on MD5 hash.
    /// </summary>
    private static bool AreFilesIdentical(string filePath1, string filePath2)
    {
        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;
            
        // Quick check: if sizes are different, files are different
        var fileInfo1 = new FileInfo(filePath1);
        var fileInfo2 = new FileInfo(filePath2);
        if (fileInfo1.Length != fileInfo2.Length)
            return false;
            
        // Compare MD5 hashes
        return ComputeFileHash(filePath1) == ComputeFileHash(filePath2);
    }
    
    /// <summary>
    /// Handle duplicate filename by comparing content and renaming if needed.
    /// Returns the final file path to use.
    /// </summary>
    private static string HandleDuplicateFile(string originalPath, string newFilePath)
    {
        if (!File.Exists(originalPath))
        {
            // No duplicate, just return the new path
            return newFilePath;
        }
        
        // Check if files are identical
        if (AreFilesIdentical(originalPath, newFilePath))
        {
            // Same content, delete the new file and return the original path
            if (File.Exists(newFilePath) && newFilePath != originalPath)
            {
                File.Delete(newFilePath);
            }
            return originalPath;
        }
        
        // Different content, rename the new file with a GUID
        var directory = Path.GetDirectoryName(newFilePath) ?? "./";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newFilePath);
        var extension = Path.GetExtension(newFilePath);
        var uniqueFileName = $"{fileNameWithoutExt}_{Guid.NewGuid():N}{extension}";
        var uniquePath = Path.Combine(directory, uniqueFileName);
        
        if (File.Exists(newFilePath) && newFilePath != uniquePath)
        {
            File.Move(newFilePath, uniquePath);
        }
        
        return uniquePath;
    }
}
