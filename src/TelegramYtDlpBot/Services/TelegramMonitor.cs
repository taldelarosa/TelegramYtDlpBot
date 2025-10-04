namespace TelegramYtDlpBot.Services;

public class TelegramMonitor : ITelegramMonitor
{
    public bool IsConnected => throw new NotImplementedException();

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SetReactionAsync(long messageId, string emoji, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
