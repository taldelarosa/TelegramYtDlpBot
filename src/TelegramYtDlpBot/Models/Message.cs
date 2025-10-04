namespace TelegramYtDlpBot.Models;

/// <summary>
/// In-memory model representing a Telegram message being processed.
/// </summary>
public class Message
{
    public long MessageId { get; init; }
    public long ChannelId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public IReadOnlyList<string> ExtractedUrls { get; init; } = Array.Empty<string>();
    public ProcessingState State { get; set; } = ProcessingState.New;
}

public enum ProcessingState
{
    New,
    UrlsExtracted,
    Queued,
    Processing,
    Completed,
    Failed
}
