using Microsoft.Data.Sqlite;

namespace TelegramYtDlpBot.Persistence;

public class StateManager : IStateManager
{
    private readonly string _connectionString;
    private readonly string _schemaPath;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public StateManager(string databasePath, string schemaPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        _schemaPath = schemaPath;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            // Read schema SQL
            var schemaSql = await File.ReadAllTextAsync(_schemaPath, cancellationToken);

            // Execute schema SQL
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Enable WAL mode for better concurrency
            await using var walCommand = connection.CreateCommand();
            walCommand.CommandText = "PRAGMA journal_mode=WAL;";
            await walCommand.ExecuteNonQueryAsync(cancellationToken);

            // Execute schema (CREATE TABLE IF NOT EXISTS is idempotent)
            await using var schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = schemaSql;
            await schemaCommand.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task SaveJobAsync(Models.DownloadJob job, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO DownloadJobs (JobId, MessageId, Url, Status, CreatedAt, CompletedAt, ErrorMessage, OutputPath, RetryCount)
            VALUES (@JobId, @MessageId, @Url, @Status, @CreatedAt, @CompletedAt, @ErrorMessage, @OutputPath, @RetryCount);";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@JobId", job.JobId.ToString());
        command.Parameters.AddWithValue("@MessageId", job.MessageId);
        command.Parameters.AddWithValue("@Url", job.Url);
        command.Parameters.AddWithValue("@Status", job.Status.ToString());
        command.Parameters.AddWithValue("@CreatedAt", job.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@CompletedAt", job.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", job.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OutputPath", job.OutputPath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", job.RetryCount);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Models.DownloadJob?> GetNextQueuedJobAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT JobId, MessageId, Url, Status, CreatedAt, CompletedAt, ErrorMessage, OutputPath, RetryCount
            FROM DownloadJobs
            WHERE Status = 'Queued'
            ORDER BY CreatedAt ASC
            LIMIT 1;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Models.DownloadJob
            {
                JobId = Guid.Parse(reader.GetString(0)),
                MessageId = reader.GetInt64(1),
                Url = reader.GetString(2),
                Status = Enum.Parse<Models.JobStatus>(reader.GetString(3)),
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                CompletedAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
                OutputPath = reader.IsDBNull(7) ? null : reader.GetString(7),
                RetryCount = reader.GetInt32(8)
            };
        }

        return null;
    }

    public async Task UpdateJobStatusAsync(Guid jobId, Models.JobStatus status, string? errorMessage = null, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE DownloadJobs
            SET Status = @Status,
                CompletedAt = @CompletedAt,
                ErrorMessage = @ErrorMessage,
                OutputPath = @OutputPath
            WHERE JobId = @JobId;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@JobId", jobId.ToString());
        command.Parameters.AddWithValue("@Status", status.ToString());
        command.Parameters.AddWithValue("@CompletedAt", status == Models.JobStatus.Completed || status == Models.JobStatus.Failed 
            ? DateTime.UtcNow.ToString("O") 
            : (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OutputPath", outputPath ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IncrementRetryCountAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE DownloadJobs
            SET RetryCount = RetryCount + 1
            WHERE JobId = @JobId;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@JobId", jobId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Models.QueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                SUM(CASE WHEN Status = 'Queued' THEN 1 ELSE 0 END) as Queued,
                SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) as InProgress,
                SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) as Completed,
                SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as Failed
            FROM DownloadJobs;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Models.QueueStats
            {
                QueuedCount = reader.GetInt32(0),
                InProgressCount = reader.GetInt32(1),
                CompletedCount = reader.GetInt32(2),
                FailedCount = reader.GetInt32(3)
            };
        }

        return new Models.QueueStats();
    }

    public async Task<bool> IsMessageProcessedAsync(long messageId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT 1 FROM ProcessedMessages WHERE MessageId = @MessageId;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@MessageId", messageId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    public async Task SaveProcessedMessageAsync(long messageId, long channelId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT OR IGNORE INTO ProcessedMessages (MessageId, ChannelId, ProcessedAt)
            VALUES (@MessageId, @ChannelId, @ProcessedAt);";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@MessageId", messageId);
        command.Parameters.AddWithValue("@ChannelId", channelId);
        command.Parameters.AddWithValue("@ProcessedAt", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetStateValueAsync(string key, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Value FROM AppState WHERE Key = @Key;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Key", key);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task SetStateValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO AppState (Key, Value, UpdatedAt)
            VALUES (@Key, @Value, @UpdatedAt)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value, UpdatedAt = @UpdatedAt;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public void Dispose()
    {
        _initLock.Dispose();
        SqliteConnection.ClearAllPools(); // Clean up connection pool
    }
}
