using System.ComponentModel.DataAnnotations;

namespace TelegramYtDlpBot.Configuration;

public class BotConfiguration
{
    public TelegramConfig Telegram { get; set; } = new();
    public YtDlpConfig YtDlp { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
    public EmojiConfig Emojis { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}

public class TelegramConfig
{
    [Required]
    public string BotToken { get; set; } = string.Empty;
    
    [Required]
    [Range(1, long.MaxValue)]
    public long ChannelId { get; set; }
    
    public int PollingIntervalSeconds { get; set; } = 2;
}

public class YtDlpConfig
{
    [Required]
    public YtDlpMode Mode { get; set; } = YtDlpMode.Local;
    
    public string ExecutablePath { get; set; } = "/usr/local/bin/yt-dlp";
    
    public string? RemoteApiUrl { get; set; }
    
    public string? RemoteApiKey { get; set; }
    
    public string Quality { get; set; } = "bestvideo+bestaudio/best";
    
    public string OutputTemplate { get; set; } = "%(uploader)s/%(upload_date)s/%(title)s.%(ext)s";
    
    public string? ConfigFilePath { get; set; }
    
    public int TimeoutMinutes { get; set; } = 60;
}

public class StorageConfig
{
    [Required]
    public string DownloadPath { get; set; } = "/data/downloads";
    
    [Required]
    public string DatabasePath { get; set; } = "/data/state.db";
    
    public int MinimumFreeSpaceGB { get; set; } = 1;
}

public class EmojiConfig
{
    public string Seen { get; set; } = "👀";
    public string Processing { get; set; } = "⚙️";
    public string Complete { get; set; } = "✅";
    public string Error { get; set; } = "❌";
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Seen)) throw new ArgumentException("Seen emoji cannot be empty");
        if (string.IsNullOrWhiteSpace(Processing)) throw new ArgumentException("Processing emoji cannot be empty");
        if (string.IsNullOrWhiteSpace(Complete)) throw new ArgumentException("Complete emoji cannot be empty");
        if (string.IsNullOrWhiteSpace(Error)) throw new ArgumentException("Error emoji cannot be empty");
    }
}

public class LoggingConfig
{
    public string MinimumLevel { get; set; } = "Information";
}

public enum YtDlpMode
{
    Local,
    Remote
}
