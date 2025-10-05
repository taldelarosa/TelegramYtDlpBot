using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Simple HTTP health check endpoint that responds to /health requests.
/// Listens on port 8080 and returns 200 OK with basic service status.
/// </summary>
public class HealthCheckService : BackgroundService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly int _port;
    private HttpListener? _listener;

    public HealthCheckService(ILogger<HealthCheckService> logger, int port = 8080)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check endpoint starting on port {Port}...", _port);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{_port}/");

        try
        {
            _listener.Start();
            _logger.LogInformation("Health check endpoint listening on http://localhost:{Port}/health", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for incoming request
                    var contextTask = _listener.GetContextAsync();
                    var completedTask = await Task.WhenAny(contextTask, Task.Delay(1000, stoppingToken));

                    if (completedTask != contextTask)
                    {
                        // Timeout, check cancellation and loop again
                        continue;
                    }

                    var context = await contextTask;
                    
                    // Handle the request
                    await HandleRequestAsync(context, stoppingToken);
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Operation aborted
                {
                    // Listener is stopping
                    break;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error handling health check request");
                }
            }
        }
        catch (HttpListenerException ex)
        {
            _logger.LogError(ex, "Failed to start health check listener on port {Port}", _port);
        }
        finally
        {
            _listener?.Stop();
            _listener?.Close();
            _logger.LogInformation("Health check endpoint stopped");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        _logger.LogDebug("Health check request: {Method} {Url}", request.HttpMethod, request.Url?.AbsolutePath);

        try
        {
            // Only respond to /health endpoint
            if (request.Url?.AbsolutePath == "/health")
            {
                var responseData = Encoding.UTF8.GetBytes("{\"status\":\"healthy\",\"service\":\"TelegramYtDlpBot\"}");
                response.StatusCode = 200;
                response.ContentType = "application/json";
                response.ContentLength64 = responseData.Length;
                await response.OutputStream.WriteAsync(responseData, cancellationToken);
            }
            else
            {
                // 404 for other paths
                response.StatusCode = 404;
                var notFoundData = Encoding.UTF8.GetBytes("{\"error\":\"Not found\"}");
                response.ContentType = "application/json";
                response.ContentLength64 = notFoundData.Length;
                await response.OutputStream.WriteAsync(notFoundData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing health check response");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    public override void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
        base.Dispose();
    }
}
