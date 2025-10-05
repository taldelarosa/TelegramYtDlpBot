# Telegram YT-DLP Bot

A Telegram bot that monitors a channel for messages containing video URLs and automatically downloads them using yt-dlp.

## Features

- 🤖 **Telegram Bot Integration**: Monitors a Telegram channel for new messages
- 🎬 **Automatic Video Downloads**: Extracts URLs from messages and downloads videos using yt-dlp
- 📊 **Job Queue Management**: Queues downloads and processes them with retry logic
- 💾 **SQLite State Persistence**: Tracks job status in a local database with WAL mode
- 🔄 **Emoji Status Updates**: Sets message reactions (👀 Seen, ⚙️ Processing, ✅ Complete, ❌ Error)
- 🏥 **Health Check Endpoint**: HTTP health check on port 8080
- ⚡ **Background Service**: Runs as a .NET BackgroundService with dependency injection

## Prerequisites

- .NET 8 SDK (for local development)
- **OR** Docker and Docker Compose (for containerized deployment)
- PowerShell (for setup script, local development only)
- Telegram Bot Token (from [@BotFather](https://t.me/botfather))

## Quick Start

### Option A: Docker (Recommended for Production/Unraid)

#### 1. Create Environment File

```bash
cp .env.example .env
```

Edit `.env` and set your values:

```bash
BOT_TOKEN=your_bot_token_here
CHANNEL_ID=-1001234567890
```

#### 2. Build and Run with Docker Compose

```bash
docker-compose up -d
```

The bot will:
- Automatically download yt-dlp during build
- Store the database in `./data/state.db`
- Save downloads to `./downloads/`
- Restart automatically unless stopped

#### 3. Check Logs

```bash
docker-compose logs -f telegram-ytdlp-bot
```

#### 4. Stop the Bot

```bash
docker-compose down
```

#### Unraid Deployment

For Unraid servers:

1. Navigate to **Docker** tab in Unraid UI
2. Click **Add Container**
3. Configure with these settings:
   - **Name**: `telegram-ytdlp-bot`
   - **Repository**: Build the image first or use your own registry
   - **Network Type**: `bridge`
   - **Port Mappings**: None required (bot doesn't expose ports)
   - **Path Mappings**:
     - Container Path: `/app/data` → Host Path: `/mnt/user/appdata/telegram-ytdlp-bot/data`
     - Container Path: `/app/downloads` → Host Path: `/mnt/user/downloads/telegram-videos`
   - **Environment Variables**:
     - `BOT_TOKEN`: Your bot token
     - `CHANNEL_ID`: Your channel ID
     - `DOTNET_ENVIRONMENT`: `Production`

Alternatively, use the Community Applications plugin and search for "telegram-ytdlp-bot" (if published).

### Option B: Local Development

### Option B: Local Development

### 1. Download yt-dlp

Run the setup script to download yt-dlp.exe:

```powershell
.\setup-ytdlp.ps1
```

This will download the latest yt-dlp.exe to the `tools/` directory.

### 2. Configure the Bot

Copy the configuration template and update with your settings:

```powershell
cp appsettings.json.template src/TelegramYtDlpBot/appsettings.json
```

Edit `src/TelegramYtDlpBot/appsettings.json` and set:

- `BotToken`: Your Telegram bot token from @BotFather
- `ChannelId`: The numeric ID of your Telegram channel (use [@userinfobot](https://t.me/userinfobot))

### 3. Run the Bot

```powershell
cd src/TelegramYtDlpBot
dotnet run
```

The bot will start monitoring your channel and processing video URLs automatically.

## Configuration

### Full Configuration Options

```json
{
  "BotConfiguration": {
    "Telegram": {
      "BotToken": "YOUR_BOT_TOKEN_HERE",
      "ChannelId": 0,
      "PollingIntervalSeconds": 2
    },
    "YtDlp": {
      "Mode": "Local",
      "ExecutablePath": "../../tools/yt-dlp.exe",
      "Quality": "bestvideo+bestaudio/best",
      "OutputTemplate": "%(uploader)s/%(upload_date)s/%(title)s.%(ext)s",
      "TimeoutMinutes": 60
    },
    "Storage": {
      "DownloadPath": "./downloads",
      "DatabasePath": "./data/state.db",
      "MinimumFreeSpaceGB": 1
    },
    "Emojis": {
      "Seen": "👀",
      "Processing": "⚙️",
      "Complete": "✅",
      "Error": "❌"
    },
    "Logging": {
      "MinimumLevel": "Information"
    },
    "HealthCheck": {
      "Port": 8080,
      "Path": "/health"
    }
  }
}
```

### Configuration Details

- **Telegram**:
  - `BotToken`: Your bot token from @BotFather (required)
  - `ChannelId`: Numeric channel ID where the bot monitors messages (required)
  - `PollingIntervalSeconds`: How often to poll for new messages (default: 2)

- **YtDlp**:
  - `Mode`: Must be "Local" (remote API not yet implemented)
  - `ExecutablePath`: Path to yt-dlp.exe (relative to project root)
  - `Quality`: Video quality setting (default: best quality)
  - `OutputTemplate`: File naming template (supports yt-dlp variables)
  - `TimeoutMinutes`: Maximum time for a download (default: 60)

- **Storage**:
  - `DownloadPath`: Where to save downloaded videos
  - `DatabasePath`: SQLite database location
  - `MinimumFreeSpaceGB`: Minimum free disk space required

- **Emojis**: Reaction emojis for different job states

- **HealthCheck**: HTTP endpoint configuration for monitoring

## Architecture

### Components

```
┌─────────────────┐
│ TelegramMonitor │  Polls Telegram API, raises MessageReceived events
└────────┬────────┘
         │
         v
┌─────────────────┐
│ DownloadWorker  │  BackgroundService orchestrating the workflow
└────────┬────────┘
         │
         ├──> UrlExtractor      (Extracts URLs from messages)
         ├──> DownloadQueue     (Manages job queue)
         ├──> LocalYtDlpExecutor (Executes yt-dlp)
         └──> StateManager      (Persists job state to SQLite)
```

### Services

- **TelegramMonitor**: Polls Telegram Bot API for new messages
- **UrlExtractor**: Regex-based URL extraction from message text
- **DownloadQueue**: In-memory job queue with thread-safe operations
- **LocalYtDlpExecutor**: Process wrapper for yt-dlp with cancellation support
- **StateManager**: SQLite database with async operations and WAL mode
- **DownloadWorker**: Main orchestration service (BackgroundService)
- **HealthCheckService**: HTTP listener for health checks

## Development

### Building Docker Image Locally

```bash
docker build -t telegram-ytdlp-bot:latest .
```

### Running with Custom Configuration

You can override settings using environment variables in docker-compose.yml:

```yaml
environment:
  - BotConfiguration__YtDlp__Quality=1080p
  - BotConfiguration__YtDlp__OutputTemplate=%(title)s.%(ext)s
```

### Running Tests

```powershell
dotnet test
```

All 40 tests should pass (100% pass rate).

### Project Structure

```
TelegramYtDlpBot/
├── src/
│   └── TelegramYtDlpBot/
│       ├── Models/          # Domain models (DownloadJob, BotConfiguration, etc.)
│       ├── Persistence/     # StateManager (SQLite)
│       ├── Services/        # All service implementations
│       ├── Program.cs       # Entry point with DI setup
│       └── appsettings.json # Configuration
├── tests/
│   └── TelegramYtDlpBot.Tests/
│       ├── Unit/           # Unit tests for all services
│       └── Integration/    # Integration tests for workflows
├── tools/
│   └── yt-dlp.exe         # Downloaded by setup-ytdlp.ps1
└── setup-ytdlp.ps1        # Setup script
```

## Workflow

1. **Message Received**: TelegramMonitor detects new message in channel
2. **URL Extraction**: UrlExtractor finds video URLs in message text
3. **Job Creation**: DownloadWorker creates DownloadJob and saves to StateManager
4. **Emoji: Seen (👀)**: Bot sets "seen" reaction on message
5. **Job Enqueue**: Job added to DownloadQueue
6. **Emoji: Processing (⚙️)**: Bot updates to "processing" reaction
7. **Download**: LocalYtDlpExecutor runs yt-dlp to download video
8. **Completion**: 
   - Success: Emoji set to ✅, job marked Complete
   - Failure: Emoji set to ❌, job marked Failed (max 3 retries)

## Health Check

The bot exposes a health check endpoint:

```bash
curl http://localhost:8080/health
```

Response:
```json
{
  "status": "healthy"
}
```

## Troubleshooting

### Docker Issues

**Container won't start:**
- Check logs: `docker-compose logs telegram-ytdlp-bot`
- Verify `.env` file exists with correct values
- Ensure volumes have correct permissions

**Downloads not persisting:**
- Check volume mounts in `docker-compose.yml`
- Verify host directories exist and are writable
- On Unraid, ensure paths are under `/mnt/user/`

**Container can't connect to Telegram:**
- Verify `BOT_TOKEN` is correct
- Check firewall rules allow outbound HTTPS
- Ensure DNS resolution is working in container

### Bot Not Starting

- Verify your bot token is correct in `appsettings.json`
- Check that the channel ID is correct (use @userinfobot)
- Ensure yt-dlp.exe exists in `tools/` directory

### Downloads Failing

- Check yt-dlp.exe is executable and working: `.\tools\yt-dlp.exe --version`
- Verify you have write permissions to the download directory
- Check disk space meets `MinimumFreeSpaceGB` requirement

### Tests Failing

- Ensure yt-dlp.exe is in the `tools/` directory
- Run `.\setup-ytdlp.ps1` if yt-dlp is missing
- Check that all dependencies are restored: `dotnet restore`

## License

This project is provided as-is for educational and personal use.

## Additional Documentation

- **[DOCKER.md](DOCKER.md)** - Complete Docker reference and configuration guide
- **[UNRAID.md](UNRAID.md)** - Step-by-step Unraid server deployment guide
- **[DOCKER-SUMMARY.md](DOCKER-SUMMARY.md)** - Quick Docker overview and next steps
- **[DEPLOYMENT-CHECKLIST.md](DEPLOYMENT-CHECKLIST.md)** - Comprehensive deployment checklist
- **[QUICK-REFERENCE.md](QUICK-REFERENCE.md)** - Essential commands and troubleshooting

## Contributing

This is a TDD-developed project. All features should have corresponding tests before implementation.

Current test coverage: 40/40 tests passing (100%)
