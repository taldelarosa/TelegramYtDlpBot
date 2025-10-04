using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramYtDlpBot.Services;

/// <summary>
/// Monitors a Telegram channel for new messages and manages emoji reactions.
/// </summary>
public class TelegramMonitor : ITelegramMonitor
{
    private readonly ITelegramBotClient? _botClient;
    private readonly ILogger<TelegramMonitor>? _logger;
    private readonly long _channelId;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Constructor for production use with real bot client.
    /// </summary>
    public TelegramMonitor(ITelegramBotClient botClient, long channelId, ILogger<TelegramMonitor>? logger = null)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _channelId = channelId;
        _logger = logger;
    }

    /// <summary>
    /// Parameterless constructor for testing.
    /// </summary>
    public TelegramMonitor()
    {
        // For testing - will not connect
        _botClient = null;
        _channelId = 0;
        _logger = null;
    }

    /// <inheritdoc />
    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        if (_botClient == null)
        {
            throw new InvalidOperationException("Bot client not initialized. Use constructor with ITelegramBotClient.");
        }

        _logger?.LogInformation("Starting Telegram monitoring for channel {ChannelId}", _channelId);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.ChannelPost },
            ThrowPendingUpdates = true
        };

        try
        {
            // Test connection
            var me = await _botClient.GetMeAsync(cancellationToken);
            _logger?.LogInformation("Connected as bot: {BotUsername}", me.Username);
            _isConnected = true;

            // Start receiving updates
            await _botClient.ReceiveAsync(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger?.LogError(ex, "Error during monitoring");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetReactionAsync(long messageId, string emoji, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji, nameof(emoji));

        if (_botClient == null)
        {
            _logger?.LogWarning("Cannot set reaction: bot client not initialized");
            return false;
        }

        // Note: Telegram.Bot 19.0.0 doesn't support SetMessageReactionAsync yet
        // This is a placeholder that logs the intent but always returns false
        _logger?.LogDebug("Would set reaction {Emoji} on message {MessageId} (not yet supported)", emoji, messageId);
        
        await Task.CompletedTask; // Satisfy async requirement
        return false; // Reactions not yet supported in this version
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            // Handle channel posts or regular messages
            var message = update.ChannelPost ?? update.Message;
            
            if (message == null || message.Chat.Id != _channelId)
            {
                return Task.CompletedTask;
            }

            // Only process text messages
            if (message.Type != MessageType.Text || string.IsNullOrEmpty(message.Text))
            {
                return Task.CompletedTask;
            }

            _logger?.LogInformation("Received message {MessageId}: {Text}", message.MessageId, message.Text);

            // Raise event
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs
            {
                MessageId = message.MessageId,
                ChannelId = message.Chat.Id,
                Text = message.Text,
                Timestamp = message.Date
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling update");
        }

        return Task.CompletedTask;
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Telegram API Error [{apiEx.ErrorCode}]: {apiEx.Message}",
            _ => exception.Message
        };

        _logger?.LogError(exception, "Polling error: {ErrorMessage}", errorMessage);

        // Don't throw - let polling continue with backoff
        return Task.CompletedTask;
    }
}

