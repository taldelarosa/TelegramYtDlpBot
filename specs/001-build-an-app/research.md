# Research: Telegram Channel URL Monitor with yt-dlp Integration

**Feature**: 001-build-an-app  
**Date**: 2025-10-04  
**Phase**: 0 - Outline & Research

---

## Research Questions Resolved

### 1. Telegram Bot API Library Selection

**Decision**: Use `Telegram.Bot` NuGet package (official .NET client)

**Rationale**:
- Official library maintained by Telegram team
- Comprehensive Bot API coverage including reactions
- Strong typing with C# models
- Active maintenance and .NET 8 support
- Built-in long polling and webhook support
- Excellent documentation and community support

**Alternatives Considered**:
- TeleSharp: Less maintained, incomplete API coverage
- Custom HTTP client: Too much manual work, error-prone
- Python telethon: Wrong language stack

**Implementation Notes**:
- Use `ITelegramBotClient` interface for testability
- Long polling via `bot.StartReceiving()` for simplicity (webhooks require public endpoint)
- Reaction API: `bot.SetMessageReactionAsync()` for emoji updates

---

### 2. yt-dlp Integration Strategy

**Decision**: Support both local CLI execution and remote API calls via strategy pattern

**Rationale**:
- Users have varying infrastructure (local docker vs dedicated yt-dlp server)
- Strategy pattern allows runtime mode switching based on configuration
- Both modes share common interface for download operations
- Local mode: Simple Process.Start() with stdout/stderr capture
- Remote mode: HTTP client to self-hosted yt-dlp API

**Alternatives Considered**:
- Local only: Limits flexibility for users with existing yt-dlp servers
- Remote only: Forces infrastructure dependency
- Python embedding: Complex, memory overhead, deployment issues

**Implementation Notes**:
- Create `IYtDlpExecutor` interface
- `LocalYtDlpExecutor`: Uses `System.Diagnostics.Process`
- `RemoteYtDlpExecutor`: Uses `HttpClient` to call REST API
- Configuration determines which implementation is injected via DI

---

### 3. SQLite Schema Design

**Decision**: Three core tables with indexes for performance

**Rationale**:
- Minimal schema for MVP: DownloadJobs, ProcessedMessages, AppState
- Foreign key from DownloadJobs → ProcessedMessages for traceability
- Indexes on MessageId and Status columns for O(log n) lookups
- Single-file embedded database simplifies deployment
- No migrations library needed initially (EF Core overkill for 3 tables)

**Schema**:
```sql
CREATE TABLE ProcessedMessages (
    MessageId INTEGER PRIMARY KEY,
    ChannelId INTEGER NOT NULL,
    ProcessedAt TEXT NOT NULL,
    UrlCount INTEGER NOT NULL
);

CREATE TABLE DownloadJobs (
    JobId TEXT PRIMARY KEY,
    MessageId INTEGER NOT NULL,
    Url TEXT NOT NULL,
    Status TEXT NOT NULL CHECK(Status IN ('Queued','InProgress','Completed','Failed')),
    CreatedAt TEXT NOT NULL,
    CompletedAt TEXT,
    ErrorMessage TEXT,
    OutputPath TEXT,
    FOREIGN KEY (MessageId) REFERENCES ProcessedMessages(MessageId)
);
CREATE INDEX idx_jobs_status ON DownloadJobs(Status);
CREATE INDEX idx_jobs_created ON DownloadJobs(CreatedAt);

CREATE TABLE AppState (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
-- Stores: LastMessageId, ConfigVersion
```

**Alternatives Considered**:
- In-memory only: Fails reliability requirements
- PostgreSQL/MySQL: Overkill for single-instance deployment
- JSON files: Poor query performance, no transactions

---

### 4. Configuration Management (12-Factor Pattern)

**Decision**: appsettings.json with environment variable overrides via .NET Configuration API

**Rationale**:
- .NET's `Microsoft.Extensions.Configuration` handles merging automatically
- Standard pattern: appsettings.json → appsettings.{Environment}.json → environment variables
- Environment variables override file config (precedence order built-in)
- Docker-friendly: Pass env vars without rebuilding image
- Type-safe binding to strongly-typed C# POCOs via `IOptions<T>`

**Configuration Structure**:
```json
{
  "Telegram": {
    "BotToken": "",
    "ChannelId": 0
  },
  "YtDlp": {
    "Mode": "Local",
    "ExecutablePath": "/usr/local/bin/yt-dlp",
    "RemoteApiUrl": "",
    "Quality": "bestvideo+bestaudio/best",
    "OutputTemplate": "%(uploader)s/%(upload_date)s/%(title)s.%(ext)s"
  },
  "Storage": {
    "DownloadPath": "/data/downloads",
    "DatabasePath": "/data/state.db"
  },
  "Emojis": {
    "Seen": "👀",
    "Processing": "⚙️",
    "Complete": "✅",
    "Error": "❌"
  },
  "Logging": {
    "MinimumLevel": "Information"
  }
}
```

**Environment Variable Overrides**:
- Format: `Telegram__BotToken`, `YtDlp__Mode`, `Storage__DownloadPath`
- Docker: `-e Telegram__BotToken=$TOKEN`
- Unraid: Define in template XML as environment variables

**Alternatives Considered**:
- Environment variables only: Poor documentation, scattered config
- Config file only: Not 12-factor compliant, requires image rebuilds
- External config service: Over-engineered for single-instance deployment

---

### 5. Health Check Implementation

**Decision**: ASP.NET Core minimal API with `/health` endpoint

**Rationale**:
- Built-in health checks via `Microsoft.Extensions.Diagnostics.HealthChecks`
- Standard HTTP endpoint for Docker HEALTHCHECK and Unraid monitoring
- Can check: Telegram API connectivity, SQLite database accessibility, disk space
- Lightweight: No need for full MVC, just minimal API
- Returns 200 OK (healthy) or 503 Service Unavailable (unhealthy)

**Health Checks**:
1. **Telegram Connectivity**: Call `bot.GetMeAsync()` with timeout
2. **Database**: Open SQLite connection and execute simple query
3. **Disk Space**: Check download path has >1GB free space
4. **Queue Health**: Check if queue processing is stuck (last job > 10 min)

**Alternatives Considered**:
- No health check: Fails Unraid monitoring requirements
- Custom TCP socket: Non-standard, requires custom monitoring
- File-based healthcheck: Polling overhead, race conditions

---

### 6. Emoji Configuration Approach

**Decision**: Hardcoded defaults with optional config file override

**Rationale**:
- 90% of users will use standard emojis (👀⚙️✅❌)
- Config file provides override mechanism for customization
- No UI needed for emoji selection (too complex for MVP)
- Validation: Ensure emojis are unique to avoid ambiguity

**Implementation**:
```csharp
public class EmojiConfiguration
{
    public string Seen { get; set; } = "👀";
    public string Processing { get; set; } = "⚙️";
    public string Complete { get; set; } = "✅";
    public string Error { get; set; } = "❌";
    
    public void Validate()
    {
        var emojis = new[] { Seen, Processing, Complete, Error };
        if (emojis.Distinct().Count() != 4)
            throw new InvalidOperationException("Emojis must be unique");
    }
}
```

---

### 7. Retry Mechanism for Failed Downloads

**Decision**: Automatic retry with exponential backoff (max 3 attempts)

**Rationale**:
- Transient network errors are common (yt-dlp API timeouts, rate limits)
- Exponential backoff prevents hammering failed endpoints
- Max 3 attempts balances reliability vs. infinite loops
- After 3 failures → mark as Failed, log error, apply error emoji

**Retry Schedule**:
- Attempt 1: Immediate (initial download)
- Attempt 2: After 30 seconds (exponential base: 30s × 2^0)
- Attempt 3: After 60 seconds (exponential base: 30s × 2^1)
- Final: Mark Failed, log, emoji ❌

**Implementation**:
- Use Polly library for resilience (retry + circuit breaker)
- Retry on: HTTP 5xx, network timeouts, transient SQLite errors
- Do NOT retry on: HTTP 4xx (bad URL), yt-dlp format errors (unsupported site)

---

### 8. Logging Format and Destination

**Decision**: Structured JSON logging via Serilog to stdout (Docker best practice)

**Rationale**:
- Structured logs enable parsing by log aggregators (Loki, Elasticsearch)
- Stdout logging is Docker standard (captured by container runtime)
- JSON format: `{"@t":"timestamp","@l":"level","message":"text","properties":{}}`
- Serilog enrichers: Add ProcessId, MachineName, ThreadId automatically
- Configurable minimum level via appsettings.json or env var

**Log Sinks**:
- Primary: Console (stdout) with JSON formatter
- Optional: File sink for local debugging (disabled in production)

**Log Levels**:
- ERROR: Download failures, Telegram API errors, SQLite exceptions
- WARN: Retry attempts, unsupported URLs, rate limit warnings
- INFO: Message processed, download started/completed, app lifecycle
- DEBUG: URL extraction details, config values (sanitized), queue state

**Alternatives Considered**:
- Plaintext logs: Harder to parse programmatically
- File-only logging: Violates Docker best practices (requires volume mount)
- Console + file: Duplicate logs, complexity

---

### 9. Post-Download Actions

**Decision**: MVP - Log completion only. Future: Webhooks, file moves

**Rationale**:
- Post-download actions are feature extensions (not core requirement)
- MVP satisfies primary goal: download with emoji feedback
- Extensibility: Design `IPostDownloadHandler` interface for future plugins
- Defer complexity: Notifications, file moves, metadata tagging come later

**MVP Behavior**:
- On completion: Update SQLite (Completed status), log success, apply ✅ emoji

**Future Extensions** (deferred):
- Webhook notification: POST to configurable endpoint with job details
- File move: Relocate completed downloads to archive location
- Metadata tagging: Embed Telegram message info into file metadata
- Telegram channel post: Send summary message to admin channel

---

### 10. yt-dlp Config File Pass-Through

**Decision**: Support via `--config-location` argument pass-through

**Rationale**:
- yt-dlp supports `--config-location` flag to load external config
- Users can provide complex yt-dlp config (proxy, cookies, format selectors)
- App adds minimal required args (URL, output template), yt-dlp config supplements
- Configuration: `YtDlp.ConfigFilePath` setting in appsettings.json

**Implementation**:
```csharp
var args = new List<string> { url };
if (!string.IsNullOrEmpty(config.YtDlp.ConfigFilePath))
    args.Add($"--config-location {config.YtDlp.ConfigFilePath}");
args.Add($"-o {outputTemplate}");
args.Add(config.YtDlp.Quality);
```

**Alternatives Considered**:
- No config file support: Limits power users
- Parse and merge config: Too complex, fragile, yt-dlp handles it

---

### 11. Secrets Management for Remote yt-dlp API

**Decision**: Optional API key via environment variable `YtDlp__RemoteApiKey`

**Rationale**:
- Not all self-hosted yt-dlp servers require authentication
- If required: Standard HTTP header `Authorization: Bearer {token}`
- Environment variable follows 12-factor pattern (consistent with bot token)
- Null/empty API key → no Authorization header sent

**Implementation**:
```csharp
if (!string.IsNullOrEmpty(config.YtDlp.RemoteApiKey))
{
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", config.YtDlp.RemoteApiKey);
}
```

---

### 12. Metrics Exposure

**Decision**: MVP - No metrics endpoint. Future: Prometheus exporter

**Rationale**:
- Metrics are operational enhancement, not core functionality
- Health check endpoint satisfies basic monitoring needs
- Prometheus integration adds complexity (metrics library, scraping config)
- Defer to post-MVP when operational patterns are established

**MVP Monitoring**:
- Health check: `/health` endpoint (200 OK vs 503)
- Logs: Structured JSON with counts, durations, errors
- SQLite queries: Count jobs by status for manual metrics

**Future Extensions** (deferred):
- Prometheus metrics: Job queue length, download duration histogram, error rate
- Grafana dashboard: Visualize throughput, latency, failures
- OpenTelemetry: Distributed tracing if multi-service architecture emerges

---

## Technology Stack Summary

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| Language | C# | 12 | Main application logic |
| Runtime | .NET | 8 LTS | Cross-platform runtime |
| Bot API | Telegram.Bot | 19.x | Telegram integration |
| Database | Microsoft.Data.Sqlite | 8.x | State persistence |
| Logging | Serilog | 3.x | Structured logging |
| Resilience | Polly | 8.x | Retry policies |
| Testing | xUnit + Moq + FluentAssertions | Latest | TDD framework |
| Container | Docker | 24+ | Deployment packaging |
| Platform | Linux (Alpine) | Latest | Runtime environment |

---

## Risk Mitigation Strategies

### Risk: Telegram API Rate Limits
**Mitigation**: Implement request throttling with Polly rate limiter. Cache bot info to reduce GetMe calls. Monitor rate limit headers.

### Risk: Large Download Queue Exhausts Disk
**Mitigation**: Health check monitors free space. Pause queue if <1GB free. Log warnings at 5GB threshold.

### Risk: SQLite Database Corruption
**Mitigation**: Enable WAL mode for concurrent reads. Regular VACUUM operations. Database backups via Docker volume snapshots.

### Risk: yt-dlp Version Incompatibility
**Mitigation**: Pin yt-dlp version in Dockerfile. Integration tests with specific yt-dlp version. Document version requirements.

### Risk: Long-Running Downloads Block Queue
**Mitigation**: Sequential processing is intentional (avoids resource contention). Timeout per download (configurable, default 1 hour). Cancel stuck downloads.

---

## Open Questions for Tasks Phase

These items will be addressed during task generation (/tasks command):

1. **Docker Base Image**: Alpine vs Debian slim (size vs compatibility trade-off)
2. **yt-dlp Installation Method**: APK package vs pip install vs binary download
3. **SQLite Migration Strategy**: Manual SQL scripts vs EF Core migrations
4. **Test Channel Creation**: Document steps for creating Telegram test bot + channel
5. **CI/CD Pipeline**: GitHub Actions workflow for build + test + Docker push
6. **Unraid Template Hosting**: Community Applications PR process

---

**Research Phase Complete** ✅  
**Next Command**: Proceed to Phase 1 (Design & Contracts) within /plan execution
