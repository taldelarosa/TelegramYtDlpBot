using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Background service that orchestrates the download workflow:
/// 1. Listens for Telegram messages
/// 2. Extracts YouTube/video URLs
/// 3. Enqueues download jobs
/// 4. Processes jobs using yt-dlp
/// 5. Updates job status and sets emoji reactions
/// </summary>
public class DownloadWorker : BackgroundService
{
    private readonly ITelegramMonitor _monitor;
    private readonly IUrlExtractor _urlExtractor;
    private readonly IDownloadQueue _queue;
    private readonly IYtDlpExecutor _executor;
    private readonly ILogger<DownloadWorker> _logger;
    private readonly string _outputPath;

    public DownloadWorker(
        ITelegramMonitor monitor,
        IUrlExtractor urlExtractor,
        IDownloadQueue queue,
        IYtDlpExecutor executor,
        ILogger<DownloadWorker> logger,
        string outputPath = "/downloads")
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _urlExtractor = urlExtractor ?? throw new ArgumentNullException(nameof(urlExtractor));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputPath = outputPath;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DownloadWorker starting...");

        // Subscribe to message events
        _monitor.MessageReceived += OnMessageReceived;

        try
        {
            // Start monitoring Telegram (runs in background)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _monitor.StartMonitoringAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring Telegram");
                }
            }, stoppingToken);

            // Main loop: process download queue
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue next job
                    var job = await _queue.DequeueAsync(stoppingToken);

                    if (job == null)
                    {
                        // Queue empty, wait a bit
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Processing job {JobId} for URL: {Url}", job.JobId, job.Url);

                    // Mark job as in progress
                    await _queue.MarkInProgressAsync(job.JobId, stoppingToken);

                    // Set processing emoji (⚙️)
                    await _monitor.SetReactionAsync(job.MessageId, "⚙️", stoppingToken);

                    try
                    {
                        // Execute download
                        var outputFile = await _executor.DownloadAsync(job.Url, _outputPath, stoppingToken);

                        // Mark completed
                        await _queue.MarkCompletedAsync(job.JobId, outputFile, stoppingToken);

                        // Set success emoji (✅)
                        await _monitor.SetReactionAsync(job.MessageId, "✅", stoppingToken);

                        _logger.LogInformation("Job {JobId} completed successfully: {OutputFile}", job.JobId, outputFile);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Job {JobId} canceled", job.JobId);
                        throw; // Propagate cancellation
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Job {JobId} failed: {ErrorMessage}", job.JobId, ex.Message);

                        // Mark failed
                        await _queue.MarkFailedAsync(job.JobId, ex.Message, stoppingToken);

                        // Check if we should retry
                        if (job.RetryCount < 3) // Max 3 retries
                        {
                            await _queue.RetryJobAsync(job.JobId, stoppingToken);
                            _logger.LogInformation("Job {JobId} requeued for retry ({RetryCount}/3)", job.JobId, job.RetryCount + 1);
                        }
                        else
                        {
                            // Max retries exceeded, set error emoji (❌)
                            await _monitor.SetReactionAsync(job.MessageId, "❌", stoppingToken);
                            _logger.LogWarning("Job {JobId} failed after max retries", job.JobId);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in download worker loop");
                    await Task.Delay(5000, stoppingToken); // Back off on errors
                }
            }

            _logger.LogInformation("DownloadWorker stopping...");
        }
        finally
        {
            _monitor.MessageReceived -= OnMessageReceived;
        }
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Received message {MessageId} from channel {ChannelId}: {Text}",
                e.MessageId, e.ChannelId, e.Text);

            // Extract URLs from message
            var urls = _urlExtractor.ExtractUrls(e.Text);

            if (urls.Count == 0)
            {
                _logger.LogDebug("No URLs found in message {MessageId}", e.MessageId);
                return;
            }

            _logger.LogInformation("Found {Count} URL(s) in message {MessageId}", urls.Count, e.MessageId);

            // Set "seen" emoji (👀)
            await _monitor.SetReactionAsync(e.MessageId, "👀", CancellationToken.None);

            // Enqueue download jobs for each URL
            foreach (var url in urls)
            {
                await _queue.EnqueueAsync(e.MessageId, url, CancellationToken.None);
                _logger.LogInformation("Enqueued job for URL: {Url}", url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message {MessageId}", e.MessageId);
        }
    }
}
