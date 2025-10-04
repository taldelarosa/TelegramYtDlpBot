namespace TelegramYtDlpBot.Services;

public class LocalYtDlpExecutor : IYtDlpExecutor
{
    public Task<string> DownloadAsync(string url, string outputPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
