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
public class TelegramMonitor : ITelegramMonitor, IUpdateHandler
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
            DropPendingUpdates = true
        };

        try
        {
            // Test connection
            var me = await _botClient.GetMe(cancellationToken);
            _logger?.LogInformation("Connected as bot: {BotUsername}", me.Username);
            _isConnected = true;

            // Start receiving updates
            await _botClient.ReceiveAsync(
                updateHandler: this,
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

        try
        {
            await _botClient.SetMessageReaction(
                chatId: _channelId,
                messageId: (int)messageId,
                reaction: new[] { new ReactionTypeEmoji { Emoji = emoji } },
                cancellationToken: cancellationToken
            );
            _logger?.LogDebug("Set reaction {Emoji} on message {MessageId}", emoji, messageId);
            return true;
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("message not found") || ex.Message.Contains("MESSAGE_NOT_FOUND"))
        {
            _logger?.LogWarning("Message {MessageId} not found for reaction {Emoji}", messageId, emoji);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set reaction {Emoji} on message {MessageId}", emoji, messageId);
            return false;
        }
    }

    Task IUpdateHandler.HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            // Debug: Log all updates received
            _logger?.LogDebug("Update received: Type={UpdateType}, Id={UpdateId}", update.Type, update.Id);
            
            // Handle channel posts or regular messages
            var message = update.ChannelPost ?? update.Message;
            
            if (message == null)
            {
                _logger?.LogDebug("No message in update");
                return Task.CompletedTask;
            }
            
            _logger?.LogDebug("Message from chat {ChatId} (expected {ExpectedChannelId}), Type={MessageType}", 
                message.Chat.Id, _channelId, message.Chat.Type);
            
            if (message.Chat.Id != _channelId)
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

    Task IUpdateHandler.HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
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

