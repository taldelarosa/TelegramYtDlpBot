using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TelegramYtDlpBot.Configuration;
using TelegramYtDlpBot.Persistence;
using TelegramYtDlpBot.Services;

// Build configuration from appsettings.json and environment variables
var builder = Host.CreateApplicationBuilder(args);

// Configure logging - use built-in console logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Bind configuration
var botConfig = builder.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>()
    ?? throw new InvalidOperationException("BotConfiguration not found in appsettings.json");

// Validate configuration
if (string.IsNullOrWhiteSpace(botConfig.Telegram.BotToken))
{
    throw new InvalidOperationException("BotToken must be configured via appsettings.json or environment variable");
}

if (botConfig.Telegram.ChannelId == 0)
{
    throw new InvalidOperationException("ChannelId must be configured");
}

// Register Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    return new TelegramBotClient(botConfig.Telegram.BotToken);
});

// Register services
builder.Services.AddSingleton<IStateManager>(sp =>
{
    var schemaPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "schema.sql");
    return new StateManager(botConfig.Storage.DatabasePath, schemaPath);
});

builder.Services.AddSingleton<IDownloadQueue>(sp =>
{
    var stateManager = sp.GetRequiredService<IStateManager>();
    return new DownloadQueue(stateManager);
});

builder.Services.AddSingleton<IUrlExtractor, UrlExtractor>();

builder.Services.AddSingleton<IYtDlpExecutor>(sp =>
{
    return new LocalYtDlpExecutor(botConfig.YtDlp.ExecutablePath);
});

builder.Services.AddSingleton<ITelegramMonitor>(sp =>
{
    var botClient = sp.GetRequiredService<ITelegramBotClient>();
    var logger = sp.GetRequiredService<ILogger<TelegramMonitor>>();
    return new TelegramMonitor(botClient, botConfig.Telegram.ChannelId, logger);
});

// Register DownloadWorker as hosted service
builder.Services.AddHostedService(sp =>
{
    var monitor = sp.GetRequiredService<ITelegramMonitor>();
    var urlExtractor = sp.GetRequiredService<IUrlExtractor>();
    var queue = sp.GetRequiredService<IDownloadQueue>();
    var executor = sp.GetRequiredService<IYtDlpExecutor>();
    var logger = sp.GetRequiredService<ILogger<DownloadWorker>>();
    return new DownloadWorker(monitor, urlExtractor, queue, executor, logger, botConfig.Storage.DownloadPath);
});

// Register health check endpoint
builder.Services.AddHostedService(sp =>
{
    var logger = sp.GetRequiredService<ILogger<HealthCheckService>>();
    return new HealthCheckService(logger, port: 8080);
});

// Build and run
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Initialize database
logger.LogInformation("Initializing database...");
var stateManager = app.Services.GetRequiredService<IStateManager>();
await stateManager.InitializeAsync(CancellationToken.None);

logger.LogInformation("TelegramYtDlpBot starting...");
logger.LogInformation("Monitoring channel: {ChannelId}", botConfig.Telegram.ChannelId);
logger.LogInformation("Download path: {DownloadPath}", botConfig.Storage.DownloadPath);
logger.LogInformation("Database path: {DatabasePath}", botConfig.Storage.DatabasePath);

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
