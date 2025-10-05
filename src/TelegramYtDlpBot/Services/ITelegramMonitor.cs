namespace TelegramYtDlpBot.Services;

/// <summary>
/// Monitors a Telegram channel for new messages and manages emoji reactions.
/// </summary>
public interface ITelegramMonitor
{
    /// <summary>
    /// Start monitoring the configured channel for new messages.
    /// </summary>
    /// <param name="cancellationToken">Token to stop monitoring</param>
    /// <returns>Task that completes when monitoring stops</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Apply an emoji reaction to a specific message.
    /// </summary>
    /// <param name="messageId">Target message ID</param>
    /// <param name="emoji">Emoji to apply (e.g., "👀", "⚙️", "✅", "❌")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if reaction was applied successfully, false otherwise</returns>
    Task<bool> SetReactionAsync(long messageId, string emoji, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get the current connection state.
    /// </summary>
    /// <returns>True if connected to Telegram API, false otherwise</returns>
    bool IsConnected { get; }
    
    /// <summary>
    /// Event raised when a new message is received from the monitored channel.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
}

/// <summary>
/// Event args for new message received.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public long MessageId { get; init; }
    public long ChannelId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
