# Service Contract: ITelegramMonitor

**Purpose**: Monitor Telegram channel for new messages and manage emoji reactions.

**Responsibilities**:
- Connect to Telegram Bot API using long polling
- Receive new messages from configured channel
- Apply emoji reactions to messages based on processing state
- Handle network disconnections and reconnections gracefully

---

## Interface Definition

```csharp
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
}
```

---

## Events

```csharp
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

// ITelegramMonitor raises this event
public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
```

---

## Behavior Specifications

### StartMonitoringAsync

**Preconditions**:
- Bot token must be configured
- Channel ID must be configured
- Bot must be added to target channel with read permissions

**Behavior**:
1. Initialize Telegram Bot API client with bot token
2. Start long polling for updates (offset from last processed message)
3. Filter messages: Only from configured channel
4. For each new message: Raise `MessageReceived` event
5. Continue polling until cancellation token is signaled
6. On network error: Log warning, retry after 5 seconds (exponential backoff, max 60s)
7. On cancellation: Stop polling gracefully, dispose client

**Postconditions**:
- Task completes when cancellation requested
- All resources disposed (ITelegramBotClient)

**Error Handling**:
- **Unauthorized (401)**: Log error, throw InvalidOperationException ("Bot token invalid")
- **Forbidden (403)**: Log error, throw InvalidOperationException ("Bot not in channel")
- **Network timeout**: Log warning, retry with exponential backoff
- **Other errors**: Log error, continue monitoring (resilience)

---

### SetReactionAsync

**Preconditions**:
- Connection to Telegram API established
- Message ID exists in channel
- Emoji is valid Unicode emoji

**Behavior**:
1. Call `bot.SetMessageReactionAsync(channelId, messageId, emoji)`
2. If successful: Return true
3. If error: Log warning, return false (non-fatal)

**Postconditions**:
- Emoji reaction visible on message (if true)
- No exception thrown (errors logged, not propagated)

**Error Handling**:
- **Message not found**: Log warning ("Message {messageId} not found"), return false
- **Rate limit exceeded**: Log warning, wait and retry once, return result
- **Network error**: Log warning, return false
- **Other errors**: Log error, return false

---

## Configuration Requirements

**From BotConfiguration.Telegram**:
- `BotToken` (string, required): Telegram Bot API token
- `ChannelId` (long, required): Target channel ID to monitor
- `PollingIntervalSeconds` (int, default 2): Seconds between poll requests

**From BotConfiguration.Emojis**:
- Emoji strings passed to `SetReactionAsync`

---

## Dependencies

- `Telegram.Bot` (ITelegramBotClient)
- `ILogger<TelegramMonitor>`
- `IOptions<BotConfiguration>`

---

## Testing Strategy

### Unit Tests

**Mocking**: Mock `ITelegramBotClient` to test logic without network calls

**Test Cases**:
- StartMonitoring_WithValidConfig_RaisesMessageReceivedEvent
- StartMonitoring_WithInvalidToken_ThrowsInvalidOperationException
- StartMonitoring_WithNetworkError_RetriesWithBackoff
- SetReaction_WithValidMessage_ReturnsTrue
- SetReaction_WithInvalidMessage_ReturnsFalse
- SetReaction_WithRateLimit_RetriesOnce

### Integration Tests

**Real API (optional)**: Use test bot + test channel

**Test Cases**:
- RealBot_ReceivesMessagesFromTestChannel
- RealBot_AppliesEmojiReactionsSuccessfully

---

## Performance Requirements

- **Latency**: Message detection ≤5 seconds from post time
- **Reaction Time**: Emoji applied within 1 second of state change
- **Memory**: <50MB for long polling client state
- **Throughput**: Handle 100+ messages/hour without degradation

---

## Example Usage

```csharp
public class Application
{
    private readonly ITelegramMonitor _monitor;
    private readonly IUrlExtractor _urlExtractor;
    
    public Application(ITelegramMonitor monitor, IUrlExtractor urlExtractor)
    {
        _monitor = monitor;
        _urlExtractor = urlExtractor;
        
        _monitor.MessageReceived += OnMessageReceived;
    }
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _monitor.StartMonitoringAsync(cancellationToken);
    }
    
    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        // Apply "seen" emoji immediately
        await _monitor.SetReactionAsync(e.MessageId, "👀", CancellationToken.None);
        
        // Extract URLs and queue downloads
        var urls = _urlExtractor.ExtractUrls(e.Text);
        // ... queue processing logic
    }
}
```

---

**Contract Ownership**: TelegramMonitor service implementation  
**Review Status**: Draft - pending implementation
